using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnTaskInsteadOfAwaitingItAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ReturnTaskInsteadOfAwaitingIt,
        title: "Return the task instead of awaiting it",
        messageFormat: "Return the task instead of awaiting it",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReturnTaskInsteadOfAwaitingIt));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var ctx = new AnalyzerContext(context.Compilation);
            if (!ctx.IsValid)
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IMethodBodyOperation)context.Operation;
                if (context.ContainingSymbol is IMethodSymbol method)
                    ctx.AnalyzeFunction(context, method, operation);
            }, OperationKind.MethodBody);

            context.RegisterOperationAction(context =>
            {
                var operation = (ILocalFunctionOperation)context.Operation;
                ctx.AnalyzeFunction(context, operation.Symbol, operation);
            }, OperationKind.LocalFunction);

            context.RegisterOperationAction(context =>
            {
                var operation = (IAnonymousFunctionOperation)context.Operation;
                ctx.AnalyzeFunction(context, operation.Symbol, operation);
            }, OperationKind.AnonymousFunction);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly Compilation _compilation;
        private readonly AwaitableTypes _awaitableTypes;
        private readonly ITypeSymbol?[] _configuredAwaitableSymbols;

        public AnalyzerContext(Compilation compilation)
        {
            _compilation = compilation;
            _awaitableTypes = new AwaitableTypes(compilation);
            _configuredAwaitableSymbols =
            [
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"),
                compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1"),
            ];
        }

        public bool IsValid => _awaitableTypes.TaskSymbol is not null;

        public void AnalyzeFunction(OperationAnalysisContext context, IMethodSymbol method, IOperation functionOperation)
        {
            if (!method.IsAsync)
                return;

            if (!_awaitableTypes.IsAsyncBuildableAndNotVoid(method.ReturnType))
                return;

            var returns = new List<IReturnOperation>();
            var awaits = new List<IAwaitOperation>();
            var hasUsingDeclaration = false;

            void Collect(IOperation operation)
            {
                // Do not descend into nested functions; they are analyzed on their own
                if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
                    return;

                switch (operation)
                {
                    case IReturnOperation returnOperation:
                        returns.Add(returnOperation);
                        break;
                    case IAwaitOperation awaitOperation:
                        awaits.Add(awaitOperation);
                        break;
                    case IUsingDeclarationOperation:
                        hasUsingDeclaration = true;
                        break;
                }

                foreach (var child in operation.GetChildOperations())
                {
                    Collect(child);
                }
            }

            foreach (var child in functionOperation.GetChildOperations())
            {
                Collect(child);
            }

            if (awaits.Count == 0)
                return;

            var returnsWithValue = returns.Where(r => r.ReturnedValue is not null).ToList();
            if (returnsWithValue.Count > 0)
            {
                AnalyzeReturningFunction(context, method, functionOperation, returnsWithValue, awaits, hasUsingDeclaration);
            }
            else
            {
                AnalyzeVoidFunction(context, method, functionOperation, awaits);
            }
        }

        // Task<T>/ValueTask<T>: every return must be "return await X;" and there must be no other await in the method
        private void AnalyzeReturningFunction(OperationAnalysisContext context, IMethodSymbol method, IOperation functionOperation, List<IReturnOperation> returnsWithValue, List<IAwaitOperation> awaits, bool hasUsingDeclaration)
        {
            if (hasUsingDeclaration)
                return;

            // Every await must be the value of a return statement, otherwise it cannot be removed
            foreach (var awaitOperation in awaits)
            {
                if (!IsReturnValue(awaitOperation))
                    return;
            }

            foreach (var returnOperation in returnsWithValue)
            {
                var value = returnOperation.ReturnedValue!;
                while (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }

                if (value is not IAwaitOperation awaitOperation)
                    return;

                if (!IsDirectlyReturnable(awaitOperation, method.ReturnType))
                    return;

                if (IsInProtectedRegion(returnOperation, functionOperation))
                    return;
            }

            foreach (var returnOperation in returnsWithValue)
            {
                var value = returnOperation.ReturnedValue!;
                while (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }

                context.ReportDiagnostic(Rule, value);
            }
        }

        // Task/ValueTask returning void: only report the simple "await X;" whole-body case
        private void AnalyzeVoidFunction(OperationAnalysisContext context, IMethodSymbol method, IOperation functionOperation, List<IAwaitOperation> awaits)
        {
            if (awaits.Count != 1)
                return;

            var awaitOperation = awaits[0];
            if (awaitOperation.Parent is not IExpressionStatementOperation)
                return;

            if (awaitOperation.Parent.Parent is not IBlockOperation { Operations.Length: 1 } block)
                return;

            if (!ReferenceEquals(block.Parent, functionOperation))
                return;

            if (!IsDirectlyReturnable(awaitOperation, method.ReturnType))
                return;

            context.ReportDiagnostic(Rule, awaitOperation);
        }

        private bool IsDirectlyReturnable(IAwaitOperation awaitOperation, ITypeSymbol returnType)
        {
            var taskOperation = awaitOperation.Operation;
            if (taskOperation is IInvocationOperation { Instance: { } instance, Type: { } configuredType } &&
                configuredType.OriginalDefinition.IsEqualToAny(_configuredAwaitableSymbols))
            {
                taskOperation = instance;
            }

            if (taskOperation.Type is not { } taskType)
                return false;

            var conversion = _compilation.ClassifyConversion(taskType, returnType);
            return conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference);
        }

        private static bool IsReturnValue(IOperation operation)
        {
            var parent = operation.Parent;
            while (parent is IConversionOperation)
            {
                parent = parent.Parent;
            }

            return parent is IReturnOperation;
        }

        private static bool IsInProtectedRegion(IOperation operation, IOperation functionOperation)
        {
            foreach (var ancestor in operation.Ancestors())
            {
                if (ReferenceEquals(ancestor, functionOperation))
                    break;

                if (ancestor is ITryOperation or IUsingOperation or ILockOperation)
                    return true;
            }

            return false;
        }
    }
}
