using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

        // The identifiers of the scope that receives the statements of the else clauses, computed on first use
        Dictionary<string, List<TextSpan>>? scopeIdentifiers = null;
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

            if (!HasUsingLocalDeclaration(elseClause) && !HasConflictingLocalIdentifiers(currentIfStatement.Statement, elseClause.Statement) && !HasConflictingIdentifiersInEnclosingScope(ifStatement, elseClause, ref scopeIdentifiers))
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

    /// <summary>
    /// Removing the else clause moves its statements to the scope that contains the 'if' statement, so the locals they declare
    /// must not conflict with the identifiers of this scope (CS0136, CS0841, or a member that the local would hide).
    /// </summary>
    private static bool HasConflictingIdentifiersInEnclosingScope(IfStatementSyntax rootIfStatement, ElseClauseSyntax elseClause, ref Dictionary<string, List<TextSpan>>? scopeIdentifiers)
    {
        HashSet<string>? movedNames = null;
        if (elseClause.Statement is IfStatementSyntax elseIfStatement)
        {
            // The else clauses of the following 'if' statements of the chain are analyzed on their own
            AddExpressionVariableNames(elseIfStatement.Condition, ref movedNames);
        }
        else
        {
            foreach (var child in AvoidUsingRedundantElseAnalyzerCommon.GetElseClauseChildren(elseClause))
            {
                AddNamesDeclaredInContainingScope(child, ref movedNames);
            }
        }

        if (movedNames is null)
            return false;

        // The whole "if / else if / else" chain ends up in the block that contains it. When the chain is not in a block,
        // the code fixer creates one that only contains the chain.
        scopeIdentifiers ??= GetIdentifiers(rootIfStatement.Parent is BlockSyntax block ? block : rootIfStatement);
        foreach (var name in movedNames)
        {
            if (!scopeIdentifiers.TryGetValue(name, out var spans))
                continue;

            foreach (var span in spans)
            {
                if (!elseClause.Span.Contains(span))
                    return true;
            }
        }

        return false;
    }

    private static Dictionary<string, List<TextSpan>> GetIdentifiers(SyntaxNode node)
    {
        var result = new Dictionary<string, List<TextSpan>>(System.StringComparer.Ordinal);
        foreach (var token in node.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken))
                continue;

            // The name of a member access cannot refer to a local
            if (token.Parent is SimpleNameSyntax name && name.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == name)
                continue;

            if (token.Parent is SimpleNameSyntax { Parent: MemberBindingExpressionSyntax })
                continue;

            if (!result.TryGetValue(token.ValueText, out var spans))
            {
                spans = [];
                result.Add(token.ValueText, spans);
            }

            spans.Add(token.Span);
        }

        return result;
    }

    /// <summary>
    /// Adds the names that <paramref name="statement"/> declares in the scope that contains it.
    /// The names declared in a nested scope (block, loop, lambda) are not added, as they are not moved to another scope.
    /// </summary>
    private static void AddNamesDeclaredInContainingScope(SyntaxNode statement, ref HashSet<string>? names)
    {
        switch (statement)
        {
            case BlockSyntax:
            case ForStatementSyntax:
            case CommonForEachStatementSyntax:
            case UsingStatementSyntax:
            case FixedStatementSyntax:
            case WhileStatementSyntax:
            case DoStatementSyntax:
            case TryStatementSyntax:
            case CheckedStatementSyntax:
            case UnsafeStatementSyntax:
                break;

            case LocalFunctionStatementSyntax localFunction:
                AddName(localFunction.Identifier, ref names);
                break;

            case LabeledStatementSyntax labeledStatement:
                AddName(labeledStatement.Identifier, ref names);
                AddNamesDeclaredInContainingScope(labeledStatement.Statement, ref names);
                break;

            case IfStatementSyntax ifStatement:
                AddExpressionVariableNames(ifStatement.Condition, ref names);

                // The else clause of a nested 'if' statement can be removed too, which moves its locals to the same scope
                if (ifStatement.Else is not null)
                {
                    foreach (var child in AvoidUsingRedundantElseAnalyzerCommon.GetElseClauseChildren(ifStatement.Else))
                    {
                        AddNamesDeclaredInContainingScope(child, ref names);
                    }
                }

                break;

            case SwitchStatementSyntax switchStatement:
                AddExpressionVariableNames(switchStatement.Expression, ref names);
                break;

            default:
                AddExpressionVariableNames(statement, ref names);
                break;
        }
    }

    private static void AddExpressionVariableNames(SyntaxNode node, ref HashSet<string>? names)
    {
        foreach (var child in node.DescendantNodesAndSelf(descendIntoChildren: n => n == node || n is not (StatementSyntax or AnonymousFunctionExpressionSyntax)))
        {
            if (child is VariableDeclaratorSyntax variableDeclarator)
            {
                AddName(variableDeclarator.Identifier, ref names);
            }
            else if (child is SingleVariableDesignationSyntax singleVariableDesignation)
            {
                AddName(singleVariableDesignation.Identifier, ref names);
            }
        }
    }

    private static void AddName(SyntaxToken identifier, ref HashSet<string>? names)
    {
        names ??= new HashSet<string>(System.StringComparer.Ordinal);
        names.Add(identifier.ValueText);
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
