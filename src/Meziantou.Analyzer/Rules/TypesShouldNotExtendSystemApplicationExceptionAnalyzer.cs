using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypesShouldNotExtendSystemApplicationExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.TypesShouldNotExtendSystemApplicationException,
            title: "Types should not extend System.ApplicationException",
            messageFormat: "Types should not extend System.ApplicationException",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TypesShouldNotExtendSystemApplicationException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var compilation = ctx.Compilation;
                var type = compilation.GetTypeByMetadataName("System.ApplicationException");

                if (type != null)
                {
                    ctx.RegisterSyntaxNodeAction(_ => Analyze(_, type), SyntaxKind.ClassDeclaration);
                }
            });
        }

        private void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol applicationExceptionType)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            if (node == null)
                return;

            var symbol = context.SemanticModel.GetDeclaredSymbol(node);
            if (symbol == null)
                return;

            if (symbol.InheritsFrom(applicationExceptionType))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, node.GetLocation()));
            }
        }
    }
}
