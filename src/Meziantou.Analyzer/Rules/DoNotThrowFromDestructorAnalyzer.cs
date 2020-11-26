using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotThrowFromDestructorAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotThrowFromDestructor,
            title: "Do not throw from a destructor",
            messageFormat: "Do not throw from a destructor",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotThrowFromDestructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeDestructor, SyntaxKind.DestructorDeclaration);
        }

        private static void AnalyzeDestructor(SyntaxNodeAnalysisContext context)
        {
            var node = (DestructorDeclarationSyntax)context.Node;
            foreach (var throwStatement in node.DescendantNodes().Where(IsThrowStatement))
            {
                context.ReportDiagnostic(s_rule, throwStatement);
            }
        }

        private static bool IsThrowStatement(SyntaxNode node) => node.IsKind(SyntaxKind.ThrowStatement);
    }
}
