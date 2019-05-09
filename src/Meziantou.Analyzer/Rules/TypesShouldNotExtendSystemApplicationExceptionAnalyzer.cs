using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypesShouldNotExtendSystemApplicationExceptionAnalyzer : DiagnosticAnalyzer
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
                    ctx.RegisterSymbolAction(_ => Analyze(_, type), SymbolKind.NamedType);
                }
            });
        }

        private static void Analyze(SymbolAnalysisContext context, INamedTypeSymbol applicationExceptionType)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.InheritsFrom(applicationExceptionType))
            {
                context.ReportDiagnostic(s_rule, symbol);
            }
        }
    }
}
