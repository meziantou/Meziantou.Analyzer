using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseServerCertificateValidationCallbackAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseServerCertificateValidationCallback,
        title: "Do not write your own certificate validation method",
        messageFormat: "Do not write your own certificate validation method",
        RuleCategories.Security,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseServerCertificateValidationCallback));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var symbols = new List<ISymbol>();

            var servicePointManagerSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Net.ServicePointManager");
            if (servicePointManagerSymbol is not null)
            {
                symbols.AddIfNotNull(servicePointManagerSymbol.GetMembers("ServerCertificateValidationCallback").FirstOrDefault());
            }

            var httpClientHandlerSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Net.Http.HttpClientHandler");
            if (httpClientHandlerSymbol is not null)
            {
                symbols.AddIfNotNull(httpClientHandlerSymbol.GetMembers("ServerCertificateCustomValidationCallback").FirstOrDefault());
            }

            if (symbols.Count != 0)
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
            context.ReportDiagnostic(Rule, operation);
        }
    }
}
