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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

            if (!HasUsingLocalDeclaration(elseClause) && !HasConflictingLocalIdentifiers(currentIfStatement.Statement, elseClause.Statement))
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

    private static bool HasConflictingLocalIdentifiers(SyntaxNode thenStatement, SyntaxNode elseStatement)
    {
        // In an "else if" chain the else statement holds every following branch, so walking it is expensive.
        // The intersection is empty as soon as the 'then' branch declares nothing, which is the common case,
        // so collect the 'then' identifiers first and only walk the else statement when one can collide.
        HashSet<string>? thenLocalIdentifiers = null;
        foreach (var identifier in FindLocalIdentifiersIn(thenStatement))
        {
            thenLocalIdentifiers ??= new HashSet<string>(System.StringComparer.Ordinal);
            thenLocalIdentifiers.Add(identifier);
        }

        if (thenLocalIdentifiers is null)
            return false;

        foreach (var identifier in FindLocalIdentifiersIn(elseStatement))
        {
            if (thenLocalIdentifiers.Contains(identifier))
                return true;
        }

        return false;
    }

    private static IEnumerable<string> FindLocalIdentifiersIn(SyntaxNode node)
    {
        foreach (var child in node.DescendantNodes())
        {
#pragma warning disable IDE0010 // Add missing cases
            switch (child)
            {
                case VariableDeclaratorSyntax variableDeclarator:
                    yield return variableDeclarator.Identifier.Text;
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    yield return localFunction.Identifier.Text;
                    break;

                case SingleVariableDesignationSyntax singleVariableDesignation:
                    yield return singleVariableDesignation.Identifier.Text;
                    break;
            }
#pragma warning restore IDE0010 // Add missing cases
        }
    }
}
