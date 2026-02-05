using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingNotNullWhenAttributeOnEquals,
        title: "Equals method should use [NotNullWhen(true)] on the parameter",
        messageFormat: "Equals method should use [NotNullWhen(true)] on parameter '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingNotNullWhenAttributeOnEquals));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var notNullWhenAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullWhenAttribute");
            if (notNullWhenAttributeSymbol is null)
                return;

            var iequatableOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");

            ctx.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, notNullWhenAttributeSymbol, iequatableOfTSymbol), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol notNullWhenAttributeSymbol, INamedTypeSymbol? iequatableOfTSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.Name != nameof(object.Equals))
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
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass.IsEqualTo(notNullWhenAttributeSymbol))
            {
                if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is true)
                {
                    // Already has the attribute with the correct value
                    return;
                }
            }
        }

        // Report diagnostic
        context.ReportDiagnostic(Rule, parameter, parameter.Name);
    }
}
