#if CSHARP10_OR_GREATER
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordClassDeclarationShouldBeImplicitAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RecordClassDeclarationShouldBeImplicit,
        title: "Record should not use explicit 'class' keyword",
        messageFormat: "Record should not be declared with explicit 'class' keyword",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RecordClassDeclarationShouldBeImplicit));

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

        // Check if this is a record with the explicit 'class' keyword
        if (recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.ClassKeyword))
        {
            // This is an explicit record class - report diagnostic on the 'class' keyword
            context.ReportDiagnostic(Rule, recordDeclaration.ClassOrStructKeyword);
        }
    }
}
#endif