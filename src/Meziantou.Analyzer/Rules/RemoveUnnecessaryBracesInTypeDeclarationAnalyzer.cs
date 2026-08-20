using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.StructDeclaration);
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
        if (!CanUseSemicolonTypeDeclaration(typeDeclaration, languageVersion))
            return false;

        if (typeDeclaration.Members.Count != 0)
            return false;

        if (typeDeclaration.OpenBraceToken.IsMissing || typeDeclaration.CloseBraceToken.IsMissing || typeDeclaration.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
            return false;

        return !ContainsCommentOrDirectiveInBraces(typeDeclaration);
    }

    private static bool CanUseSemicolonTypeDeclaration(TypeDeclarationSyntax typeDeclaration, LanguageVersion languageVersion)
    {
        if (typeDeclaration is RecordDeclarationSyntax)
            return true;

        if (!languageVersion.IsCSharp12OrGreater())
            return false;

        if (typeDeclaration is ClassDeclarationSyntax or StructDeclarationSyntax)
            return true;

        return false;
    }

    private static bool ContainsCommentOrDirectiveInBraces(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.OpenBraceToken.TrailingTrivia
            .Concat(typeDeclaration.CloseBraceToken.LeadingTrivia)
            .Any(static trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));
    }
}
