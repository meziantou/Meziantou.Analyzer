using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseEventHandlerOfTAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseEventHandlerOfT,
            title: "Use EventHandler<T>",
            messageFormat: "{0}",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEventHandlerOfT));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                ctx.RegisterSymbolAction(analyzerContext.AnalyzeSymbol, SymbolKind.Event);
            });
        }

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                EventArgsSymbol = compilation.GetTypeByMetadataName("System.EventArgs");
            }

            public INamedTypeSymbol? EventArgsSymbol { get; }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                var symbol = (IEventSymbol)context.Symbol;
                var method = (symbol.Type as INamedTypeSymbol)?.DelegateInvokeMethod;
                if (method == null)
                    return;

                if (IsValidSignature(method, out var message))
                    return;

                if (symbol.IsInterfaceImplementation())
                    return;

                context.ReportDiagnostic(s_rule, symbol, message);
            }

            private bool IsValidSignature(IMethodSymbol methodSymbol, [NotNullWhen(false)] out string? message)
            {
                if (!methodSymbol.ReturnsVoid)
                {
                    message = "The delegate must return void";
                    return false;
                }

                if (methodSymbol.Arity != 0)
                {
                    message = "The delegate must not be a generic method";
                    return false;
                }

                if (methodSymbol.Parameters.Length != 2)
                {
                    message = "The delegate must have 2 parameters";
                    return false;
                }

                if (!methodSymbol.Parameters[0].Type.IsObject())
                {
                    message = "The first parameter must be of type object";
                    return false;
                }

                if (!methodSymbol.Parameters[1].Type.IsOrInheritFrom(EventArgsSymbol))
                {
                    message = "The second parameter must be of type 'System.EventArgs' or a derived type";
                    return false;
                }

                message = null;
                return true;
            }
        }
    }
}
