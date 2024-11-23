using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PublicRecordAnalyzer : DiagnosticAnalyzer
    {
        private const string SealedRecordTitle = "Annotate public record with 'sealed'";
        private const string SealedRecordMessageFormat = "Public record '{0}' should be annotated with 'sealed'.";
        private const string SealedRecordDescription = "Eyooo, annotate public record with 'sealed.'";
        private const string Category = "Convention";

        private static readonly DiagnosticDescriptor SealedRecordRule = new DiagnosticDescriptor(RuleIdentifiers.PublicRecordShouldBeSealed,
            SealedRecordTitle, SealedRecordMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: SealedRecordDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SealedRecordRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeRecordDeclaration, SyntaxKind.RecordDeclaration);
        }

        private void AnalyzeRecordDeclaration(SyntaxNodeAnalysisContext context)
        {
            var recordDeclaration = (RecordDeclarationSyntax)context.Node;

            if (recordDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !recordDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                var diagnostic = Diagnostic.Create(SealedRecordRule, recordDeclaration.Identifier.GetLocation(),
                    recordDeclaration.Identifier.Text);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}