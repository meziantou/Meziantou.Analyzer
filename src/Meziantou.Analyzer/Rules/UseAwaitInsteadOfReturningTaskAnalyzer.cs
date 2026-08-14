using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
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
            context.RegisterOperationAction(context => AnalyzeReturn(context, awaitableTypes, operationUtilities), OperationKind.Return);
        });
    }

    private static void AnalyzeReturn(OperationAnalysisContext context, AwaitableTypes awaitableTypes, OperationUtilities operationUtilities)
    {
        var operation = (IReturnOperation)context.Operation;

        // Ignore "yield return" and "return;"
        if (operation.ReturnedValue is null)
            return;

        var value = operation.ReturnedValue;

        // Do not report when returning null or default as they cannot be awaited
        var unwrappedValue = value;
        while (unwrappedValue is IConversionOperation conversion)
        {
            unwrappedValue = conversion.Operand;
        }

        if (unwrappedValue is IDefaultValueOperation or IThrowOperation)
            return;

        if (unwrappedValue.ConstantValue is { HasValue: true, Value: null })
            return;

        // The returned value must be awaitable
        if (!awaitableTypes.IsAwaitable(value.Type, operation.SemanticModel!, value.Syntax.SpanStart))
            return;

        var function = GetEnclosingFunction(operation, context);
        if (function is null || function.IsAsync)
            return;

        // The function must return an awaitable type that can be used with the 'async' keyword
        if (!awaitableTypes.IsAsyncBuildableAndNotVoid(function.ReturnType))
            return;

        // 'await' cannot be used in expression trees
        if (operationUtilities.IsInExpressionContext(operation))
            return;

        // Only report when the return statement is the whole method body ("return X;" or "=> X"). Otherwise,
        // converting "return X;" to "await X;" for a non-generic task would require rewriting the control flow,
        // and the rule is meant for simple task-forwarding methods.
        if (!IsSoleBodyStatement(operation))
            return;

        context.ReportDiagnostic(Rule, value);
    }

    private static bool IsSoleBodyStatement(IReturnOperation operation)
    {
        if (operation.Parent is not IBlockOperation block)
            return false;

        if (block.Operations.Length != 1)
            return false;

        return block.Parent is IMethodBodyOperation or ILocalFunctionOperation or IAnonymousFunctionOperation;
    }

    private static IMethodSymbol? GetEnclosingFunction(IOperation operation, OperationAnalysisContext context)
    {
        foreach (var ancestor in operation.Ancestors())
        {
            switch (ancestor)
            {
                case IAnonymousFunctionOperation anonymousFunction:
                    return anonymousFunction.Symbol;

                case ILocalFunctionOperation localFunction:
                    return localFunction.Symbol;
            }
        }

        return context.ContainingSymbol as IMethodSymbol;
    }
}
