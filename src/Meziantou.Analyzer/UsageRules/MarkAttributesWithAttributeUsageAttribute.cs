using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MarkAttributesWithAttributeUsageAttribute : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute,
            title: "Mark attributes with AttributeUsageAttribute",
            messageFormat: "Mark attributes with AttributeUsageAttribute",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var attributeType = context.Compilation.GetTypeByMetadataName("System.Attribute");
            var attributeUsageAttributeType = context.Compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
            if (attributeType == null || attributeUsageAttributeType == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (symbol == null)
                return;

            if (!symbol.InheritsFrom(attributeType) || symbol.HasAttribute(attributeUsageAttributeType))
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
        }
    }
}
