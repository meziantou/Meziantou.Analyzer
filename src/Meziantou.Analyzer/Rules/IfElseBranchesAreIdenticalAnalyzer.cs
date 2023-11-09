using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IfElseBranchesAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.IfElseBranchesAreIdentical,
        title: "Both if and else branch have identical code",
        messageFormat: "Both if and else branch have identical code",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.IfElseBranchesAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);
    }

    private void AnalyzeConditional(OperationAnalysisContext context)
    {
        var operation = (IConditionalOperation)context.Operation;
        if (operation.WhenFalse != null)
        {
            var whenTrue = operation.WhenTrue.Syntax;
            var whenFalse = operation.WhenFalse.Syntax;
            if (whenFalse.IsEquivalentTo(whenTrue, topLevel: false))
            {
                context.ReportDiagnostic(s_rule, operation);
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
                        context.ReportDiagnostic(s_rule, operation);
                    }
                }
            }
        }
    }
}
