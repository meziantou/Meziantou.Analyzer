using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConditionalCompilationBranchesAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ConditionalCompilationBranchesAreIdentical,
        title: "Conditional compilation branches have identical code",
        messageFormat: "Conditional compilation branches have identical code",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ConditionalCompilationBranchesAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var sourceText = context.Tree.GetText(context.CancellationToken);
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (!trivia.HasStructure)
                continue;

            if (trivia.GetStructure() is IfDirectiveTriviaSyntax ifDirective)
            {
                AnalyzeDirectiveGroup(context, sourceText, ifDirective);
            }
        }
    }

    private static void AnalyzeDirectiveGroup(SyntaxTreeAnalysisContext context, SourceText sourceText, IfDirectiveTriviaSyntax ifDirective)
    {
        var relatedDirectives = ifDirective.GetRelatedDirectives();
        List<DirectiveTriviaSyntax> branchDirectives = [];
        DirectiveTriviaSyntax? endIfDirective = null;
        foreach (var directive in relatedDirectives)
        {
            if (directive.IsKind(SyntaxKind.IfDirectiveTrivia) || directive.IsKind(SyntaxKind.ElifDirectiveTrivia) || directive.IsKind(SyntaxKind.ElseDirectiveTrivia))
            {
                branchDirectives.Add(directive);
            }
            else if (directive.IsKind(SyntaxKind.EndIfDirectiveTrivia))
            {
                endIfDirective = directive;
            }
        }

        if (endIfDirective is null || branchDirectives.Count < 2)
            return;

        Dictionary<string, DirectiveTriviaSyntax> previousBranchBySignature = new(StringComparer.Ordinal);
        for (var i = 0; i < branchDirectives.Count; i++)
        {
            var startDirective = branchDirectives[i];
            var endDirective = i + 1 < branchDirectives.Count ? branchDirectives[i + 1] : endIfDirective;
            var branchSpan = TextSpan.FromBounds(startDirective.FullSpan.End, endDirective.FullSpan.Start);
            var signature = ComputeBranchSignature(sourceText, branchSpan);
            if (previousBranchBySignature.ContainsKey(signature))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, startDirective.GetLocation()));
            }
            else
            {
                previousBranchBySignature.Add(signature, startDirective);
            }
        }
    }

    private static string ComputeBranchSignature(SourceText sourceText, TextSpan span)
    {
        var text = sourceText.ToString(span);
        var tokens = SyntaxFactory.ParseTokens(text);
        var builder = new StringBuilder();
        foreach (var token in tokens)
        {
            if (token.IsKind(SyntaxKind.EndOfFileToken))
                continue;

            builder.Append(token.RawKind);
            builder.Append(':');
            builder.Append(token.Text);
            builder.Append(';');
        }

        return builder.ToString();
    }
}
