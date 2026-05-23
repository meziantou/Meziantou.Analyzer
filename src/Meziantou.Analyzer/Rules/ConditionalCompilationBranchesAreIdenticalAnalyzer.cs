using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

            if (trivia.GetStructure() is IfDirectiveTriviaSyntax ifDirective &&
                ConditionalCompilationBranchesAreIdenticalCommon.TryCreateBranchGroup(ifDirective, sourceText, out var group))
            {
                AnalyzeDirectiveGroup(context, group);
            }
        }
    }

    private static void AnalyzeDirectiveGroup(SyntaxTreeAnalysisContext context, ConditionalCompilationBranchesAreIdenticalCommon.BranchGroup group)
    {
        Dictionary<string, DirectiveTriviaSyntax> previousBranchBySignature = new(StringComparer.Ordinal);
        foreach (var branch in group.Branches)
        {
            if (previousBranchBySignature.ContainsKey(branch.Signature))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, branch.StartDirective.GetLocation()));
            }
            else
            {
                previousBranchBySignature.Add(branch.Signature, branch.StartDirective);
            }
        }
    }
}
