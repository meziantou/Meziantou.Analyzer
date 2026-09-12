namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullableAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.NotNullIfNotNullArgumentShouldExist,
        title: "Invalid parameter name for nullable attribute",
        messageFormat: "Parameter '{0}' does not exist",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.NotNullIfNotNullArgumentShouldExist));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var type = ctx.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute");
            if (type is null)
                return;

            ctx.RegisterSymbolAction(symbolContext => AnalyzeMethod(symbolContext, type), SymbolKind.Method);
            ctx.RegisterSymbolAction(symbolContext => AnalyzeProperty(symbolContext, type), SymbolKind.Property);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, INamedTypeSymbol notNullIfNotNullAttributeTypeSymbol)
    {
        var method = (IMethodSymbol)context.Symbol;
        AnalyzeAttributes(context, notNullIfNotNullAttributeTypeSymbol, method.GetReturnTypeAttributes(), method, method);

        // The parameters of the accessors of an indexer duplicate the parameters of the indexer
        if (method.AssociatedSymbol is IPropertySymbol)
            return;

        foreach (var parameter in method.Parameters)
        {
            AnalyzeAttributes(context, notNullIfNotNullAttributeTypeSymbol, parameter.GetAttributes(), parameter, method);
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, INamedTypeSymbol notNullIfNotNullAttributeTypeSymbol)
    {
        var property = (IPropertySymbol)context.Symbol;
        AnalyzeAttributes(context, notNullIfNotNullAttributeTypeSymbol, property.GetAttributes(), property, property);
        foreach (var parameter in property.Parameters)
        {
            AnalyzeAttributes(context, notNullIfNotNullAttributeTypeSymbol, parameter.GetAttributes(), parameter, property);
        }
    }

    private static void AnalyzeAttributes(SymbolAnalysisContext context, INamedTypeSymbol notNullIfNotNullAttributeTypeSymbol, ImmutableArray<AttributeData> attributes, ISymbol symbolToReport, ISymbol declaringSymbol)
    {
        foreach (var attribute in attributes)
        {
            if (!attribute.AttributeClass.IsEqualTo(notNullIfNotNullAttributeTypeSymbol))
                continue;

            if (attribute.ConstructorArguments is [{ Value: string parameterName }] && !ParameterExists(declaringSymbol, parameterName))
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                if (location is not null)
                {
                    context.ReportDiagnostic(Rule, location, parameterName);
                }
                else
                {
                    context.ReportDiagnostic(Rule, symbolToReport, parameterName);
                }
            }
        }
    }

    private static bool ParameterExists(ISymbol declaringSymbol, string parameterName)
    {
        // For a property, the name can also be the value parameter of the setter
        var parameters = declaringSymbol switch
        {
            IMethodSymbol method => method.Parameters,
            IPropertySymbol { SetMethod: { } setMethod } => setMethod.Parameters,
            IPropertySymbol property => property.Parameters,
            _ => ImmutableArray<IParameterSymbol>.Empty,
        };

        if (parameters.Any(p => p.Name == parameterName))
            return true;

#if CSHARP14_OR_GREATER
        if (declaringSymbol.ContainingType?.ExtensionParameter?.Name == parameterName)
            return true;
#endif

        return false;
    }
}
