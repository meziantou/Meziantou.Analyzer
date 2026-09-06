namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingMaybeNullWhenAttributeOnTryGetValueAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingMaybeNullWhenAttributeOnTryGetValue,
        title: "TryGetValue method should use [MaybeNullWhen(false)] on the value parameter",
        messageFormat: "TryGetValue method should use [MaybeNullWhen(false)] on parameter '{0}'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingMaybeNullWhenAttributeOnTryGetValue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var maybeNullWhenAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute");
            if (maybeNullWhenAttributeSymbol is null)
                return;

            var idictionaryOfTSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IDictionary`2");
            if (idictionaryOfTSymbol is null)
                return;

            if (idictionaryOfTSymbol.GetMembers("TryGetValue").Length != 1)
                return;

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

                    if (namedType.FindImplementationForInterfaceMember(dictionaryTryGetValueSymbols[0]) is not IMethodSymbol implementation)
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

                    context.ReportDiagnostic(Rule, valueParameter, valueParameter.Name);
                }
            }, SymbolKind.NamedType);
        });
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
