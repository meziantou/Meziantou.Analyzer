using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class AvoidUsingRedundantElseAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidUsingRedundantElse,
        title: "Avoid using redundant else",
        messageFormat: "Avoid using redundant else",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The 'if' block contains a jump statement (break, continue, goto, return, throw, yield break). Using 'else' is redundant and needlessly maintains a higher nesting level.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidUsingRedundantElse));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        // Analyze the whole "if / else if / else" chain from the 'if' that starts it. Registering on the
        // else clause instead means every clause re-analyzes the branches above it, which is quadratic in
        // the length of the chain.
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Only the 'if' that starts the chain drives the analysis; the following ones are visited by the loop
        if (ifStatement.Parent is ElseClauseSyntax { Parent: IfStatementSyntax })
            return;

        var currentIfStatement = ifStatement;
        while (true)
        {
            var elseClause = currentIfStatement.Else;
            if (elseClause is null)
                return;

            // A branch that does not jump unconditionally makes the 'else' of every following branch
            // meaningful too, so there is nothing left to report in this chain
            if (!IsUnreachableEndpoint(context.SemanticModel.AnalyzeControlFlow(currentIfStatement.Statement)))
                return;

            // The locals whose names conflict with other identifiers don't prevent the diagnostic, as the code fix keeps the block of the else clause
            if (!HasUsingLocalDeclaration(elseClause))
            {
                context.ReportDiagnostic(Rule, elseClause.ElseKeyword);
            }

            if (elseClause.Statement is not IfStatementSyntax nextIfStatement)
                return;

            currentIfStatement = nextIfStatement;
        }
    }

    private static bool IsUnreachableEndpoint(ControlFlowAnalysis? controlFlowAnalysis)
    {
        return controlFlowAnalysis is { Succeeded: true, EndPointIsReachable: false };
    }

    /// <summary>
    /// Detects a "using statement local declaration" as a direct child of the else clause.
    /// </summary>
    /// <remarks>
    /// <c>using var charEnumerator = "".GetEnumerator();</c> is a <see cref="LocalDeclarationStatementSyntax"/> (matches),
    /// whereas <c>using (var charEnumerator = "".GetEnumerator()) { }</c> is a <see cref="UsingStatementSyntax"/> (does not match).
    /// </remarks>
    private static bool HasUsingLocalDeclaration(ElseClauseSyntax elseClause)
    {
        foreach (var child in AvoidUsingRedundantElseAnalyzerCommon.GetElseClauseChildren(elseClause))
        {
            if (child is LocalDeclarationStatementSyntax localDeclaration && localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                return true;
        }

        return false;
    }
}
