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
            title: "Do not use ServerCertificateValidationCallback",
            messageFormat: "Do not use ServerCertificateValidationCallback",
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
                var servicePointManagerSymbol = ctx.Compilation.GetTypeByMetadataName("System.Net.ServicePointManager");
                if (servicePointManagerSymbol == null)
                    return;

                var eventSymbol = servicePointManagerSymbol.GetMembers("ServerCertificateValidationCallback").FirstOrDefault();
                if (eventSymbol == null)
                    return;

                ctx.RegisterOperationAction(c => Analyze(c, eventSymbol), OperationKind.PropertyReference);
            });
        }

        private void Analyze(OperationAnalysisContext context, ISymbol eventSymbol)
        {
            var operation = (IPropertyReferenceOperation)context.Operation;
            if (operation.Property.Equals(eventSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }
}
