using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseContainsKeyInsteadOfTryGetValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseContainsKeyInsteadOfTryGetValue,
        title: "Use ContainsKey instead of TryGetValue",
        messageFormat: "Use ContainsKey instead of TryGetValue",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseContainsKeyInsteadOfTryGetValue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private INamedTypeSymbol? IReadOnlyDictionary { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
        private INamedTypeSymbol? IDictionary { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IDictionary`2");

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;

            if (operation is { TargetMethod: { Name: "TryGetValue", Parameters.Length: 2, ContainingType: not null }, Arguments: [_, { Value: IDiscardOperation }] })
            {
                foreach (var symbol in (ReadOnlySpan<INamedTypeSymbol?>)[IReadOnlyDictionary, IDictionary])
                {
                    if (symbol is not null)
                    {
                        var iface = operation.TargetMethod.ContainingType.OriginalDefinition.IsEqualTo(symbol) ? operation.TargetMethod.ContainingType : operation.TargetMethod.ContainingType.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, symbol));
                        if (iface is not null)
                        {
                            if (iface.GetMembers("TryGetValue").FirstOrDefault() is IMethodSymbol member)
                            {
                                var implementation = operation.TargetMethod.IsEqualTo(member) ? member : operation.TargetMethod.ContainingType.FindImplementationForInterfaceMember(member);
                                if (SymbolEqualityComparer.Default.Equals(operation.TargetMethod, implementation))
                                {
                                    context.ReportDiagnostic(Rule, operation);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}