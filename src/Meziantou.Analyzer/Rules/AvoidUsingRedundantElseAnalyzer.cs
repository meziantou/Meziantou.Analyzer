﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeElseClause, SyntaxKind.ElseClause);
    }

    private static void AnalyzeElseClause(SyntaxNodeAnalysisContext context)
    {
        var elseClause = (ElseClauseSyntax)context.Node;
        if (elseClause is null)
            return;

        if (elseClause.Parent is not IfStatementSyntax ifStatement)
            return;

        var thenStatement = ifStatement.Statement;
        var elseStatement = elseClause.Statement;
        if (thenStatement is null || elseStatement is null)
            return;

        // If the 'else' clause contains a "using statement local declaration" as direct child, return
        // NOTE:
        //  using var charEnumerator = "".GetEnumerator();          => LocalDeclarationStatementSyntax  (will return)
        //  using (var charEnumerator = "".GetEnumerator()) { }     => UsingStatementSyntax             (will carry on)
        var elseHasUsingStatementLocalDeclaration = AvoidUsingRedundantElseAnalyzerCommon.GetElseClauseChildren(elseClause)
            .OfType<LocalDeclarationStatementSyntax>()
            .Any(localDeclaration => localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword));
        if (elseHasUsingStatementLocalDeclaration)
            return;

        // If there are conflicting local (variable or function) declarations in 'if' and 'else' blocks, return
        var thenLocalIdentifiers = FindLocalIdentifiersIn(thenStatement);
        var elseLocalIdentifiers = FindLocalIdentifiersIn(elseStatement);
        if (thenLocalIdentifiers.Intersect(elseLocalIdentifiers, System.StringComparer.Ordinal).Any())
            return;

        var controlFlowAnalysis = context.SemanticModel.AnalyzeControlFlow(thenStatement);
        if (controlFlowAnalysis is null || !controlFlowAnalysis.Succeeded)
            return;

        if (!controlFlowAnalysis.EndPointIsReachable)
        {
            context.ReportDiagnostic(Rule, elseClause.ElseKeyword);
        }
    }

    private static IEnumerable<string> FindLocalIdentifiersIn(SyntaxNode node)
    {
        foreach (var child in node.DescendantNodes())
        {
#pragma warning disable 
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
