using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseOperatingSystemInsteadOfRuntimeInformationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseOperatingSystemInsteadOfRuntimeInformation,
        title: "Use System.OperatingSystem to check the current OS",
        messageFormat: "Use System.OperatingSystem to check the current OS",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseOperatingSystemInsteadOfRuntimeInformation));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var isOSPlatformSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform)", context.Compilation) as IMethodSymbol;
            var osPlatformSymbol = context.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.OSPlatform");
            var operatingSystemSymbol = context.Compilation.GetBestTypeByMetadataName("System.OperatingSystem");
            if (isOSPlatformSymbol is null || operatingSystemSymbol is null || osPlatformSymbol is null)
                return;

            context.RegisterOperationAction(context => AnalyzeInvocation(context, isOSPlatformSymbol, osPlatformSymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol runtimeInformationSymbol, INamedTypeSymbol osPlatformSymbol)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.Arguments.Length == 1 && SymbolEqualityComparer.Default.Equals(runtimeInformationSymbol, operation.TargetMethod))
        {
            if (operation.Arguments[0].Value is IMemberReferenceOperation access)
            {
                if (access.Member.ContainingType.IsEqualTo(osPlatformSymbol))
                {
                    context.ReportDiagnostic(s_rule, operation);
                }
            }
        }
    }
}
