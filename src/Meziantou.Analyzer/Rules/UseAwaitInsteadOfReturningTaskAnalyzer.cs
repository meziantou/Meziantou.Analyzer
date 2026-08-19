using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAwaitInsteadOfReturningTaskAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseAwaitInsteadOfReturningTask,
        title: "Use 'await' instead of returning the task",
        messageFormat: "Use 'await' instead of returning the task",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAwaitInsteadOfReturningTask));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var awaitableTypes = new AwaitableTypes(context.Compilation);
            if (awaitableTypes.TaskSymbol is null)
                return;

            var operationUtilities = new OperationUtilities(context.Compilation);

            context.RegisterOperationAction(context =>
            {
                var operation = (IMethodBodyOperation)context.Operation;
                if (context.ContainingSymbol is IMethodSymbol method)
                    AnalyzeFunction(context, method, operation, awaitableTypes, operationUtilities);
            }, OperationKind.MethodBody);

            context.RegisterOperationAction(context =>
            {
                var operation = (ILocalFunctionOperation)context.Operation;
                AnalyzeFunction(context, operation.Symbol, operation, awaitableTypes, operationUtilities);
            }, OperationKind.LocalFunction);

            context.RegisterOperationAction(context =>
            {
                var operation = (IAnonymousFunctionOperation)context.Operation;
                AnalyzeFunction(context, operation.Symbol, operation, awaitableTypes, operationUtilities);
            }, OperationKind.AnonymousFunction);
        });
    }

    private static void AnalyzeFunction(OperationAnalysisContext context, IMethodSymbol method, IOperation functionOperation, AwaitableTypes awaitableTypes, OperationUtilities operationUtilities)
    {
        if (method.IsAsync)
            return;

        // Only members that support the 'async' keyword can be reported. Property/event accessors, operators,
        // constructors, etc. cannot be async even though they may return an awaitable type.
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.LocalFunction or MethodKind.LambdaMethod or MethodKind.AnonymousFunction))
            return;

        // The function must return an awaitable type that can be used with the 'async' keyword
        if (!awaitableTypes.IsAsyncBuildableAndNotVoid(method.ReturnType))
            return;

        // 'await' cannot be used in expression trees
        if (operationUtilities.IsInExpressionContext(functionOperation))
            return;

        var returns = new List<IReturnOperation>();
        CollectReturns(functionOperation, returns);

        var hasValueToAwait = false;
        foreach (var returnOperation in returns)
        {
            if (returnOperation.ReturnedValue is null)
                continue;

            var value = returnOperation.ReturnedValue;
            var unwrappedValue = value;
            while (unwrappedValue is IConversionOperation conversion)
            {
                unwrappedValue = conversion.Operand;
            }

            // Cannot await null/default/throw. Report only when every return can be updated.
            if (unwrappedValue is IDefaultValueOperation or IThrowOperation)
                return;

            if (unwrappedValue.ConstantValue is { HasValue: true, Value: null })
                return;

            if (!awaitableTypes.IsAwaitable(value.Type, value.SemanticModel!, value.Syntax.SpanStart))
                return;

            // Do not report inside try/using (would change exception/disposal semantics) or lock/fixed
            // (where 'await' is not even allowed).
            if (IsInProtectedContext(returnOperation.Syntax, functionOperation.Syntax))
                return;

            hasValueToAwait = true;
        }

        if (!hasValueToAwait)
            return;

        foreach (var returnOperation in returns)
        {
            if (returnOperation.ReturnedValue is not null)
                context.ReportDiagnostic(Rule, returnOperation.ReturnedValue);
        }
    }

    private static bool IsInProtectedContext(SyntaxNode returnSyntax, SyntaxNode functionSyntax)
    {
        for (var node = returnSyntax.Parent; node is not null && node != functionSyntax; node = node.Parent)
        {
            if (node is TryStatementSyntax or UsingStatementSyntax or LockStatementSyntax or FixedStatementSyntax)
                return true;
        }

        return false;
    }

    private static void CollectReturns(IOperation root, List<IReturnOperation> returns)
    {
        foreach (var child in root.GetChildOperations())
        {
            CollectReturnsCore(child, returns);
        }
    }

    private static void CollectReturnsCore(IOperation operation, List<IReturnOperation> returns)
    {
        // Do not descend into nested functions; they are analyzed on their own
        if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
            return;

        if (operation is IReturnOperation returnOperation)
        {
            returns.Add(returnOperation);
        }

        foreach (var child in operation.GetChildOperations())
        {
            CollectReturnsCore(child, returns);
        }
    }
}
