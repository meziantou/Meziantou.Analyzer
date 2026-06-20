using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveUnnecessaryBracesInTypeDeclarationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RemoveUnnecessaryBracesInTypeDeclaration,
        title: "Remove unnecessary braces in type declaration",
        messageFormat: "Remove unnecessary braces in type declaration",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveUnnecessaryBracesInTypeDeclaration));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.RecordDeclaration);
#if CSHARP12_OR_GREATER
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.StructDeclaration);
#endif
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (!CanRemoveBraces(typeDeclaration, context.Compilation.GetCSharpLanguageVersion()))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, typeDeclaration.OpenBraceToken.GetLocation()));
    }

    private static bool CanRemoveBraces(TypeDeclarationSyntax typeDeclaration, LanguageVersion languageVersion)
    {
        if (!HasParameterList(typeDeclaration, languageVersion))
            return false;

        if (typeDeclaration.Members.Count != 0)
            return false;

        if (typeDeclaration.OpenBraceToken.IsMissing || typeDeclaration.CloseBraceToken.IsMissing || typeDeclaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            return false;

        return !ContainsCommentOrDirectiveInBraces(typeDeclaration);
    }

    private static bool HasParameterList(TypeDeclarationSyntax typeDeclaration, LanguageVersion languageVersion)
    {
        if (typeDeclaration is RecordDeclarationSyntax { ParameterList: not null })
            return true;

#if CSHARP12_OR_GREATER
        if (!languageVersion.IsCSharp12OrAbove())
            return false;

        if (typeDeclaration is ClassDeclarationSyntax { ParameterList: not null } or StructDeclarationSyntax { ParameterList: not null })
            return true;
#endif

        return false;
    }

    private static bool ContainsCommentOrDirectiveInBraces(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.OpenBraceToken.TrailingTrivia
            .Concat(typeDeclaration.CloseBraceToken.LeadingTrivia)
            .Any(static trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
    }
}
