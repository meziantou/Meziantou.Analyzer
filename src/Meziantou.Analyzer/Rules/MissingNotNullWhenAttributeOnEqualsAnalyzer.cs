using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
            var iequatableOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");
            var idictionaryOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IDictionary`2");

            if (idictionaryOfTSymbol != null && maybeNullWhenAttributeSymbol is not null)
            {
                var tryGetValueSymbols = idictionaryOfTSymbol.GetMembers("TryGetValue");
                if (tryGetValueSymbols.Length == 1)
                {
                    var tryGetValueSymbol = tryGetValueSymbols[0];
                    ctx.RegisterSymbolAction(context =>
                    {
                        var namedType = (INamedTypeSymbol)context.Symbol;
                        foreach (var interfaceType in namedType.AllInterfaces)
                        {
                            if (!interfaceType.ConstructedFrom.IsEqualTo(idictionaryOfTSymbol))
                                continue;

                            var dictionaryTryGetValueSymbols = interfaceType.GetMembers("TryGetValue");
                            if (dictionaryTryGetValueSymbols.Length != 1)
                                continue;

                            var implementation = namedType.FindImplementationForInterfaceMember(dictionaryTryGetValueSymbols[0]) as IMethodSymbol;
                            if (implementation is null)
                                continue;

                            if (implementation.Parameters.Length != 2)
                                continue;

                            var valueParameter = implementation.Parameters[1];

                            // Check if the parameter is an out parameter
                            if (valueParameter.RefKind != RefKind.Out)
                                continue;

                            // Check if the parameter is nullable
                            if (valueParameter.NullableAnnotation != NullableAnnotation.Annotated)
                                continue;

                            // Check if the parameter already has [MaybeNullWhen(false)] attribute
                            if (HasMaybeNullWhenAttribute(valueParameter, maybeNullWhenAttributeSymbol, expectedValue: false))
                                continue;

                            // Report diagnostic
                            context.ReportDiagnostic(TryGetValueRule, valueParameter, valueParameter.Name);
                        }
                    }, SymbolKind.NamedType);
                }


            }

            if (notNullWhenAttributeSymbol is not null)
            {
                context.RegisterSymbolAction(context =>
                {
                    var method = (IMethodSymbol)context.Symbol;
                    if (method.Name is nameof(object.Equals))
                    {
                        if (!method.ReturnType.IsBoolean())
                            return;

                        if (method.Parameters.Length != 1)
                            return;

                        if (method.IsStatic)
                            return;

                        var parameter = method.Parameters[0];

                        // Check if the parameter is nullable, this also ensures nullable annotations are enabled for the parameter
                        if (parameter.NullableAnnotation != NullableAnnotation.Annotated)
                            return;


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

                        // Check if the parameter already has [NotNullWhen(true)] attribute
                        if (HasNotNullWhenAttribute(parameter, notNullWhenAttributeSymbol, expectedValue: true))
                            return;

                        // Report diagnostic
                        context.ReportDiagnostic(EqualsRule, parameter, parameter.Name);
                    }
                }, SymbolKind.Method);
            }
        });
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
