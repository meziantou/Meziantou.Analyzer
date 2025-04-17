#if ROSLYN_4_8_OR_GREATER
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseReadOnlyStructForRefReadOnlyParametersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseReadOnlyStructForRefReadOnlyParameters,
        title: "Use readonly struct for in or ref readonly parameter",
        messageFormat: "Use readonly struct for in or ref readonly parameter",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseReadOnlyStructForRefReadOnlyParameters));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(context =>
        {
            var parameter = (IParameterSymbol)context.Symbol;
            if (!IsValidParameter(parameter))
            {
                context.ReportDiagnostic(Rule, parameter);
            }

        }, SymbolKind.Parameter);

        context.RegisterOperationAction(context =>
        {
            var operation = (ILocalFunctionOperation)context.Operation;
            var symbol = operation.Symbol;
            foreach (var parameter in symbol.Parameters)
            {
                if (!IsValidParameter(parameter))
                {
                    context.ReportDiagnostic(Rule, parameter);
                }
            }

        }, OperationKind.LocalFunction);

        context.RegisterOperationAction(ctx =>
        {
            var operation = (IArgumentOperation)ctx.Operation;
            var parameter = operation.Parameter;
            if (parameter is null)
                return;

            // Do not report non-generic types as they are reported by SymbolAction
            if (SymbolEqualityComparer.Default.Equals(parameter.OriginalDefinition.Type, parameter.Type))
                return;

            if (!IsValidParameter(parameter))
            {
                ctx.ReportDiagnostic(Rule, operation);
            }
        }, OperationKind.Argument);
    }

    private static bool IsValidParameter(IParameterSymbol parameter)
    {
        if (parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter)
        {
            if (parameter.Type is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.IsValueType && !namedTypeSymbol.IsReadOnly)
                    return false;
            }
        }

        return true;
    }
}
#endif