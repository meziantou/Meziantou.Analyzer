using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AwaitAwaitableMethodInSyncMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.AwaitAwaitableMethodInSyncMethod,
        title: "Observe result of async calls",
        messageFormat: "Observe result of async calls",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AwaitAwaitableMethodInSyncMethod));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
            {
                var awaitableTypes = new AwaitableTypes(context.Compilation);
            context.RegisterSymbolStartAction(context =>
                    {
                if (context.Symbol is IMethodSymbol method && (method.IsAsync || method.IsTopLevelStatementsEntryPointMethod()))
                            return; // Already handled by CS4014

                context.RegisterOperationAction(context => AnalyzeOperation(context, awaitableTypes), OperationKind.Invocation);
            }, SymbolKind.Method);
            });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, AwaitableTypes awaitableTypes)
    {
        var operation = (IInvocationOperation)context.Operation;

        var parent = FindStatementParent(operation);

        if (parent is null or IBlockOperation or IExpressionStatementOperation or IConditionalAccessOperation)
        {
            var semanticModel = operation.SemanticModel!;
            var position = operation.Syntax.GetLocation().SourceSpan.End;

            // While there is a check in RegisterSymbolStartAction, this is needed to handle lambda and delegates
            var enclosingSymbol = semanticModel.GetEnclosingSymbol(position, context.CancellationToken);
            if (enclosingSymbol is IMethodSymbol method && (method.IsAsync || method.IsTopLevelStatementsEntryPointMethod()))
                return;

            if (!awaitableTypes.IsAwaitable(operation.Type, semanticModel, position))
                return;

            if (parent is IExpressionStatementOperation)
            {
                context.ReportDiagnostic(s_rule, parent);
            }
            else
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }

    private static IOperation? FindStatementParent(IOperation? operation)
    {
        var parent = operation.Parent;
        while (parent is IConditionalAccessOperation { Parent: { } } grantParent)
        {
            parent = grantParent.Parent;
        }

        return parent;
    }
}
