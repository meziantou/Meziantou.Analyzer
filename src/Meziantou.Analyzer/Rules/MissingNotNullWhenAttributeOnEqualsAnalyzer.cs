using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor EqualsRule = new(
        RuleIdentifiers.MissingNotNullWhenAttributeOnEquals,
        title: "Equals method should use [NotNullWhen(true)] on the parameter",
        messageFormat: "Equals method should use [NotNullWhen(true)] on parameter '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingNotNullWhenAttributeOnEquals));

    private static readonly DiagnosticDescriptor TryGetValueRule = new(
        RuleIdentifiers.MissingNotNullWhenAttributeOnEquals,
        title: "TryGetValue method should use [MaybeNullWhen(false)] on the value parameter",
        messageFormat: "TryGetValue method should use [MaybeNullWhen(false)] on parameter '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingNotNullWhenAttributeOnEquals));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(EqualsRule, TryGetValueRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var notNullWhenAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullWhenAttribute");
            var maybeNullWhenAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute");
            if (notNullWhenAttributeSymbol is null && maybeNullWhenAttributeSymbol is null)
                return;

            var iequatableOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");
            var idictionaryOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IDictionary`2");

            ctx.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, notNullWhenAttributeSymbol, maybeNullWhenAttributeSymbol, iequatableOfTSymbol, idictionaryOfTSymbol), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol? notNullWhenAttributeSymbol, INamedTypeSymbol? maybeNullWhenAttributeSymbol, INamedTypeSymbol? iequatableOfTSymbol, INamedTypeSymbol? idictionaryOfTSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;

        // Check for Equals methods
        if (method.Name == nameof(object.Equals))
        {
            AnalyzeEqualsMethod(context, method, notNullWhenAttributeSymbol, iequatableOfTSymbol);
        }
        // Check for TryGetValue methods
        else if (method.Name == "TryGetValue")
        {
            AnalyzeTryGetValueMethod(context, method, maybeNullWhenAttributeSymbol, idictionaryOfTSymbol);
        }
    }

    private static void AnalyzeEqualsMethod(SymbolAnalysisContext context, IMethodSymbol method, INamedTypeSymbol? notNullWhenAttributeSymbol, INamedTypeSymbol? iequatableOfTSymbol)
    {
        if (notNullWhenAttributeSymbol is null)
            return;

        if (!method.ReturnType.IsBoolean())
            return;

        if (method.Parameters.Length != 1)
            return;

        if (method.IsStatic)
            return;

        var parameter = method.Parameters[0];

        // Check if it's Equals(object?) override using helper
        var isObjectEqualsOverride = false;
        if (method.IsOverride && parameter.Type.IsObject())
        {
            // Verify it's overriding object.Equals by checking the base member
            var currentMethod = method.OverriddenMethod;
            while (currentMethod is not null)
            {
                if (currentMethod.ContainingType.IsObject())
                {
                    isObjectEqualsOverride = true;
                    break;
                }
                currentMethod = currentMethod.OverriddenMethod;
            }
        }

        // Check if it's IEquatable<T>.Equals(T?) implementation using helper
        var isIEquatableEquals = false;
        if (iequatableOfTSymbol is not null && method.ContainingType is not null && !method.ContainingType.IsValueType)
        {
            if (method.IsInterfaceImplementation())
            {
                var interfaceMethod = method.GetImplementingInterfaceSymbol();
                if (interfaceMethod is not null && 
                    interfaceMethod.ContainingType is INamedTypeSymbol interfaceType &&
                    interfaceType.ConstructedFrom.IsEqualTo(iequatableOfTSymbol))
                {
                    isIEquatableEquals = true;
                }
            }
        }

        if (!isObjectEqualsOverride && !isIEquatableEquals)
            return;

        // Check if the parameter is nullable
        // This also ensures nullable annotations are enabled for the parameter
        if (parameter.NullableAnnotation != NullableAnnotation.Annotated)
            return;

        // Check if the parameter already has [NotNullWhen(true)] attribute
        if (HasNotNullWhenAttribute(parameter, notNullWhenAttributeSymbol, expectedValue: true))
            return;

        // Report diagnostic
        context.ReportDiagnostic(EqualsRule, parameter, parameter.Name);
    }

    private static void AnalyzeTryGetValueMethod(SymbolAnalysisContext context, IMethodSymbol method, INamedTypeSymbol? maybeNullWhenAttributeSymbol, INamedTypeSymbol? idictionaryOfTSymbol)
    {
        if (maybeNullWhenAttributeSymbol is null || idictionaryOfTSymbol is null)
            return;

        if (!method.ReturnType.IsBoolean())
            return;

        if (method.Parameters.Length != 2)
            return;

        if (method.IsStatic)
            return;

        // Check if it's implementing IDictionary<TKey, TValue>.TryGetValue
        if (!method.IsInterfaceImplementation())
            return;

        var interfaceMethod = method.GetImplementingInterfaceSymbol();
        if (interfaceMethod is null)
            return;

        if (interfaceMethod.ContainingType is not INamedTypeSymbol interfaceType)
            return;

        if (!interfaceType.ConstructedFrom.IsEqualTo(idictionaryOfTSymbol))
            return;

        // Check the value parameter (second parameter, typically named "value")
        var valueParameter = method.Parameters[1];

        // Check if the parameter is an out parameter
        if (valueParameter.RefKind != RefKind.Out)
            return;

        // Check if the parameter is nullable
        if (valueParameter.NullableAnnotation != NullableAnnotation.Annotated)
            return;

        // Check if the parameter already has [MaybeNullWhen(false)] attribute
        if (HasMaybeNullWhenAttribute(valueParameter, maybeNullWhenAttributeSymbol, expectedValue: false))
            return;

        // Report diagnostic
        context.ReportDiagnostic(TryGetValueRule, valueParameter, valueParameter.Name);
    }

    private static bool HasNotNullWhenAttribute(IParameterSymbol parameter, INamedTypeSymbol notNullWhenAttributeSymbol, bool expectedValue)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass.IsEqualTo(notNullWhenAttributeSymbol))
            {
                if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is bool value && value == expectedValue)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool HasMaybeNullWhenAttribute(IParameterSymbol parameter, INamedTypeSymbol maybeNullWhenAttributeSymbol, bool expectedValue)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass.IsEqualTo(maybeNullWhenAttributeSymbol))
            {
                if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is bool value && value == expectedValue)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
