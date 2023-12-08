using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IfElseBranchesAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.IfElseBranchesAreIdentical,
        title: "Both if and else branch have identical code",
        messageFormat: "Both if and else branch have identical code",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.IfElseBranchesAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);
    }

    private void AnalyzeConditional(OperationAnalysisContext context)
    {
        var operation = (IConditionalOperation)context.Operation;
        if (operation.WhenFalse is not null)
        {
            var whenTrue = operation.WhenTrue.Syntax;
            var whenFalse = operation.WhenFalse.Syntax;
            if (whenFalse.IsEquivalentTo(whenTrue, topLevel: false))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
        else if (operation.WhenTrue.Kind is OperationKind.Return or OperationKind.Branch)
        {
            var parent = operation.Parent;
            if (parent is IBlockOperation block)
            {
                var index = block.Operations.IndexOf(operation);
                if (block.Operations.Length > index + 1)
                {
                    var whenTrue = operation.WhenTrue.Syntax;
                    var whenFalse = block.Operations[index + 1].Syntax;
                    if (whenFalse.IsEquivalentTo(whenTrue, topLevel: false))
                    {
                        context.ReportDiagnostic(Rule, operation);
                    }
                }
            }
        }
    }
}
