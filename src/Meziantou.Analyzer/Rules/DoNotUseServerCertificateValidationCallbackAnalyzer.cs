using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseServerCertificateValidationCallbackAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseServerCertificateValidationCallback,
            title: "Do not write your own certificate validation method",
            messageFormat: "Do not write your own certificate validation method",
            RuleCategories.Security,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseServerCertificateValidationCallback));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var symbols = new List<ISymbol>();

                var servicePointManagerSymbol = ctx.Compilation.GetTypeByMetadataName("System.Net.ServicePointManager");
                if (servicePointManagerSymbol != null)
                {
                    symbols.AddIfNotNull(servicePointManagerSymbol.GetMembers("ServerCertificateValidationCallback").FirstOrDefault());
                }

                var httpClientHandlerSymbol = ctx.Compilation.GetTypeByMetadataName("System.Net.Http.HttpClientHandler");
                if (httpClientHandlerSymbol != null)
                {
                    symbols.AddIfNotNull(httpClientHandlerSymbol.GetMembers("ServerCertificateCustomValidationCallback").FirstOrDefault());
                }

                if (symbols.Any())
                {
                    ctx.RegisterOperationAction(c => Analyze(c, symbols), OperationKind.PropertyReference);
                }
            });
        }

        private static void Analyze(OperationAnalysisContext context, List<ISymbol> eventSymbols)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;
            if (eventSymbols.Contains(operation.Property))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }
}
