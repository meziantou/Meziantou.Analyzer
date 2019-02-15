using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStructLayoutAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MissingStructLayoutAttribute,
            title: "Add StructLayoutAttribute",
            messageFormat: "Add StructLayoutAttribute",
            RuleCategories.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingStructLayoutAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
                if (attributeType == null)
                    return;

                compilationContext.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StructDeclaration);
            });
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var declaration = (StructDeclarationSyntax)context.Node;
            if (declaration == null)
                return;

            var attributeType = context.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
            if (attributeType == null)
                return;

            var typeInfo = context.SemanticModel.GetDeclaredSymbol(declaration);
            if (typeInfo == null)
                return;

            if (typeInfo.GetAttributes().Any(attr => attributeType.Equals(attr.AttributeClass)))
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_rule, declaration.GetLocation()));
        }
    }
}
