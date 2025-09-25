#if CSHARP10_OR_GREATER
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordClassDeclarationShouldBeExplicitAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RecordClassDeclarationShouldBeExplicit,
        title: "Record should use explicit 'class' keyword",
        messageFormat: "Record should be declared with explicit 'class' keyword",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RecordClassDeclarationShouldBeExplicit));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeRecordDeclaration, SyntaxKind.RecordDeclaration);
    }

    private static void AnalyzeRecordDeclaration(SyntaxNodeAnalysisContext context)
    {
        var recordDeclaration = (RecordDeclarationSyntax)context.Node;

        // Check if this is a record without the explicit 'class' keyword
        // RecordDeclarationSyntax.ClassOrStructKeyword will be null/missing for implicit record classes
        // and will contain 'class' or 'struct' for explicit ones
        if (recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.None))
        {
            // This is an implicit record class (no 'class' or 'struct' keyword)
            // Report diagnostic on the record keyword
            context.ReportDiagnostic(Rule, recordDeclaration.Keyword);
        }
    }
}
#endif
