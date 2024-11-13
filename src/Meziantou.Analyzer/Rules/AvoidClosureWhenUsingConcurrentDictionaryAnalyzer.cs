using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidClosureWhenUsingConcurrentDictionaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary,
        title: "Use the lambda parameters instead of using a closure",
        messageFormat: "Use the lambda parameters instead of using a closure (captured variable: {0})",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionary));

    private static readonly DiagnosticDescriptor RuleFactoryArg = new(
        RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg,
        title: "Avoid closure by using an overload with the 'factoryArgument' parameter",
        messageFormat: "Avoid closure by using an overload with the 'factoryArgument' parameter (captured variable: {0})",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidClosureWhenUsingConcurrentDictionaryByUsingFactoryArg));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleFactoryArg);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.ConcurrentDictionarySymbol is null)
                return;

            ctx.RegisterOperationAction(ctx => analyzerContext.AnalyzeInvocation(ctx), OperationKind.Invocation);
        });

    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            ConcurrentDictionarySymbol = compilation.GetBestTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
            if (ConcurrentDictionarySymbol is null)
                return;

            Func2Symbol = compilation.GetBestTypeByMetadataName("System.Func`2");
            Func3Symbol = compilation.GetBestTypeByMetadataName("System.Func`3");
            Func4Symbol = compilation.GetBestTypeByMetadataName("System.Func`4");

            GetOrAddHasOverloadWithArg = ConcurrentDictionarySymbol.GetMembers("GetOrAdd").OfType<IMethodSymbol>().Any(m => m.Parameters.Any(p => p.Name == "factoryArgument"));
            AddOrUpdateHasOverloadWithArg = ConcurrentDictionarySymbol.GetMembers("AddOrUpdate").OfType<IMethodSymbol>().Any(m => m.Parameters.Any(p => p.Name == "factoryArgument"));
        }

        public INamedTypeSymbol? ConcurrentDictionarySymbol { get; }
        public INamedTypeSymbol? Func2Symbol { get; }
        public INamedTypeSymbol? Func3Symbol { get; }
        public INamedTypeSymbol? Func4Symbol { get; }
        public bool GetOrAddHasOverloadWithArg { get; }
        public bool AddOrUpdateHasOverloadWithArg { get; }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (!op.TargetMethod.ContainingSymbol.OriginalDefinition.IsEqualTo(ConcurrentDictionarySymbol))
                return;

            // Check if the key/value parameter should be used
            var handled = false;
            if (op.TargetMethod.Name is "GetOrAdd")
            {
                // a.GetOrAdd(key, (k) => key);
                if (op.Arguments.Length == 2 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(Func2Symbol))
                {
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                }
                // a.GetOrAdd(key, (k, arg) => key, arg);
                else if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(Func3Symbol))
                {
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[2]);
                }
            }
            else if (op.TargetMethod.Name is "AddOrUpdate")
            {
                // a.AddOrUpdate(key, (k) => k, (k, oldValue) => k + oldValue + 1);
                if (op.Arguments.Length == 3 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(Func2Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(Func3Symbol))
                {
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[0]);
                }
                // a.AddOrUpdate(key, newValue, (k, oldValue) => k + oldValue);
                else if (op.Arguments.Length == 3 && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(Func3Symbol))
                {
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[0]);
                }
                // a.AddOrUpdate(key, (k, arg) => k + arg, (k, oldValue, arg) => k + oldValue + arg, factoryArg);
                else if (op.Arguments.Length == 4 && op.Arguments[1].Parameter!.Type.OriginalDefinition.IsEqualTo(Func3Symbol) && op.Arguments[2].Parameter!.Type.OriginalDefinition.IsEqualTo(Func4Symbol))
                {
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[0]);
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[1].Value, op.Arguments[3]);

                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[0]);
                    handled |= DetectPotentialUsageOfLambdaParameter(context, op.Arguments[2].Value, op.Arguments[3]);
                }
            }

            if ((!handled && GetOrAddHasOverloadWithArg && op.TargetMethod.Name is "GetOrAdd") || (AddOrUpdateHasOverloadWithArg && op.TargetMethod.Name is "AddOrUpdate"))
            {
                foreach (var arg in op.Arguments)
                {
                    if (arg.Parameter!.OriginalDefinition.Type.OriginalDefinition.IsEqualToAny(Func2Symbol, Func3Symbol, Func4Symbol))
                    {
                        DetectClosure(context, arg.Value);
                    }
                }
            }
        }
    }

    private static bool DetectPotentialUsageOfLambdaParameter(OperationAnalysisContext context, IOperation argumentOperation, IArgumentOperation potentialVariableOperation)
    {
        var value = potentialVariableOperation.Value;
        ISymbol? symbol = null;

        if (value is ILocalReferenceOperation localReferenceOperation)
        {
            symbol = localReferenceOperation.Local;
        }
        else if (value is IParameterReferenceOperation parameterReferenceOperation)
        {
            symbol = parameterReferenceOperation.Parameter;
        }

        if (symbol is null)
            return false;

        if (argumentOperation is IAnonymousFunctionOperation or IDelegateCreationOperation)
        {
            var syntax = GetDataFlowArgument(argumentOperation.Syntax);
            var semanticModel = context.Operation.SemanticModel!;
            var dataFlow = semanticModel.AnalyzeDataFlow(syntax);
            foreach (var read in dataFlow.ReadInside)
            {
                if (read.IsEqualTo(symbol))
                {
                    foreach (var written in dataFlow.WrittenInside)
                    {
                        if (written.IsEqualTo(symbol))
                            return false;
                    }

                    context.ReportDiagnostic(Rule, argumentOperation, read.Name);
                    return true;
                }
            }
        }

        return false;
    }

    private static void DetectClosure(OperationAnalysisContext context, IOperation argumentOperation)
    {
        if (argumentOperation is IAnonymousFunctionOperation or IDelegateCreationOperation)
        {
            var syntax = GetDataFlowArgument(argumentOperation.Syntax);
            var semanticModel = context.Operation.SemanticModel!;
            var dataFlow = semanticModel.AnalyzeDataFlow(syntax);
            if (dataFlow.CapturedInside.Length > 0)
            {
                // A parameter can be captured inside (by another lambda)
                var parameters = GetParameters(argumentOperation);
                if (dataFlow.CapturedInside.Any(s => !parameters.Contains(s, SymbolEqualityComparer.Default)))
                {
                    context.ReportDiagnostic(RuleFactoryArg, argumentOperation, string.Join(", ", dataFlow.Captured.Select(symbol => symbol.Name)));
                }
            }
        }

        static IEnumerable<ISymbol> GetParameters(IOperation operation)
        {
            if (operation is IAnonymousFunctionOperation func)
            {
                return func.Symbol.Parameters;
            }

            if (operation is IDelegateCreationOperation delegateCreation)
            {
                return GetParameters(delegateCreation.Target);
            }

            return [];
        }
    }

    [return: NotNullIfNotNull(nameof(node))]
    private static SyntaxNode? GetDataFlowArgument(SyntaxNode? node)
    {
        if (node is null)
            return null;

        if (node is ArrowExpressionClauseSyntax expression)
        {
            return expression.Expression;
        }

        return node;
    }
}
