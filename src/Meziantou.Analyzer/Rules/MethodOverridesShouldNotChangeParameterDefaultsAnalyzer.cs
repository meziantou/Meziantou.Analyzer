using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

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
            baseSymbol = method.GetImplementedInterfaceMember();
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
                .Add(MethodOverridesShouldNotChangeParameterDefaultsAnalyzerCommon.HasDefaultValueKey, parameter.HasExplicitDefaultValue ? "true" : "false")
                .Add(MethodOverridesShouldNotChangeParameterDefaultsAnalyzerCommon.DefaultValueKey, value: parameter.HasExplicitDefaultValue ? (defaultExpressionSyntax?.ToString() ?? GetDefaultValueExpression(parameter)) : null);
        }
    }

    // The parameters declared in a referenced assembly have no syntax, so the expression is created from the constant value
    private static string? GetDefaultValueExpression(IParameterSymbol parameter)
    {
        var value = parameter.ExplicitDefaultValue;
        var type = parameter.Type;
        if (value is null)
            return type.IsReferenceType || type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T ? "null" : "default";

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType)
        {
            type = nullableType.TypeArguments[0];
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            var enumTypeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value))
                    return enumTypeName + "." + (SyntaxFacts.GetKeywordKind(field.Name) is SyntaxKind.None ? field.Name : "@" + field.Name);
            }

            return GetLiteralExpression(value) switch
            {
                null => null,
                ['-', ..] literal => $"({enumTypeName})({literal})",
                var literal => $"({enumTypeName}){literal}",
            };
        }

        return GetLiteralExpression(value);
    }

    private static string? GetLiteralExpression(object value)
    {
        return value switch
        {
            bool boolean => boolean ? "true" : "false",
            string str => SyntaxFactory.Literal(str).Text,
            char c => SyntaxFactory.Literal(c).Text,
            sbyte number => SyntaxFactory.Literal(number).Text,
            byte number => SyntaxFactory.Literal(number).Text,
            short number => SyntaxFactory.Literal(number).Text,
            ushort number => SyntaxFactory.Literal(number).Text,
            int number => SyntaxFactory.Literal(number).Text,
            uint number => SyntaxFactory.Literal(number).Text,
            long number => SyntaxFactory.Literal(number).Text,
            ulong number => SyntaxFactory.Literal(number).Text,
            decimal number => SyntaxFactory.Literal(number).Text,
            float number when float.IsNaN(number) => "float.NaN",
            float number when float.IsPositiveInfinity(number) => "float.PositiveInfinity",
            float number when float.IsNegativeInfinity(number) => "float.NegativeInfinity",
            float number => SyntaxFactory.Literal(number).Text,
            double number when double.IsNaN(number) => "double.NaN",
            double number when double.IsPositiveInfinity(number) => "double.PositiveInfinity",
            double number when double.IsNegativeInfinity(number) => "double.NegativeInfinity",
            double number => SyntaxFactory.Literal(number).Text,
            _ => null,
        };
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
