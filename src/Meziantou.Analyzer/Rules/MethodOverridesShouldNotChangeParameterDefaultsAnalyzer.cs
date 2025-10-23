using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodOverridesShouldNotChangeParameterDefaultsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults,
        title: "Method overrides should not change default values",
        messageFormat: "Method overrides should not change default values (original: {0}; current: {1})",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared || method.Parameters.Length == 0)
            return;

        if (method.ExplicitInterfaceImplementations.Length > 0)
            return;

        IMethodSymbol? baseSymbol;
        if (method.IsOverride)
        {
            baseSymbol = method.OverriddenMethod;
        }
        else
        {
            baseSymbol = method.GetImplementingInterfaceSymbol();
        }

        if (baseSymbol is null)
            return;

        foreach (var parameter in method.Parameters)
        {
            if (parameter.IsImplicitlyDeclared || parameter.IsThis)
                continue;

            var originalParameter = baseSymbol.Parameters[parameter.Ordinal];
            if (originalParameter.HasExplicitDefaultValue != parameter.HasExplicitDefaultValue)
            {
                var properties = CreateProperties(originalParameter, context.CancellationToken);
                context.ReportDiagnostic(Rule, properties, parameter, GetParameterDisplayValue(originalParameter), GetParameterDisplayValue(parameter));
            }
            else if (originalParameter.HasExplicitDefaultValue && !Equals(originalParameter.ExplicitDefaultValue, parameter.ExplicitDefaultValue))
            {
                var properties = CreateProperties(originalParameter, context.CancellationToken);
                context.ReportDiagnostic(Rule, properties, parameter, GetParameterDisplayValue(originalParameter), GetParameterDisplayValue(parameter));
            }
        }

        static ImmutableDictionary<string, string?> CreateProperties(IParameterSymbol parameter, CancellationToken cancellationToken)
        {
            ExpressionSyntax? defaultExpressionSyntax = null;
            foreach (var s in parameter.DeclaringSyntaxReferences)
            {
                var syntax = s.GetSyntax(cancellationToken);
                if (syntax is ParameterSyntax param)
                {
                    defaultExpressionSyntax ??= param.Default?.Value;
                }
            }

            return ImmutableDictionary<string, string?>.Empty
                .Add("HasDefaultValue", parameter.HasExplicitDefaultValue ? "true" : "false")
                .Add("DefaultValue", value: parameter.HasExplicitDefaultValue ? (defaultExpressionSyntax?.ToString() ?? parameter.ExplicitDefaultValue?.ToString() ?? null) : null);
        }
    }

    private static string GetParameterDisplayValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return "<no default value>";

        if (parameter.ExplicitDefaultValue is null)
        {
            return "null";
        }

        return string.Create(CultureInfo.InvariantCulture, $"'{parameter.ExplicitDefaultValue}'");
    }
}
