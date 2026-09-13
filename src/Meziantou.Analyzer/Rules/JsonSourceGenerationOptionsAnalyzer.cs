namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JsonSourceGenerationOptionsAnalyzer : DiagnosticAnalyzer
{
    private const string RespectNullableAnnotationsPropertyName = "RespectNullableAnnotations";
    private const string RespectRequiredConstructorParametersPropertyName = "RespectRequiredConstructorParameters";

    private static readonly DiagnosticDescriptor RespectNullableAnnotationsRule = new(
        RuleIdentifiers.SetRespectNullableAnnotations,
        title: "JsonSourceGenerationOptions should set RespectNullableAnnotations",
        messageFormat: "Set RespectNullableAnnotations on [JsonSourceGenerationOptions]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SetRespectNullableAnnotations));

    private static readonly DiagnosticDescriptor RespectRequiredConstructorParametersRule = new(
        RuleIdentifiers.SetRespectRequiredConstructorParameters,
        title: "JsonSourceGenerationOptions should set RespectRequiredConstructorParameters",
        messageFormat: "Set RespectRequiredConstructorParameters on [JsonSourceGenerationOptions]",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SetRespectRequiredConstructorParameters));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RespectNullableAnnotationsRule, RespectRequiredConstructorParametersRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            if (!analyzerContext.IsValid)
                return;

            compilationContext.RegisterSymbolAction(analyzerContext.AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly INamedTypeSymbol? _jsonSerializerContextSymbol = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSerializerContext");
        private readonly INamedTypeSymbol? _attributeSymbol = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute");

        // JsonSerializerDefaults.Strict, introduced in .NET 10, sets both properties
        private readonly IFieldSymbol? _strictDefaults = compilation.GetTypeByMetadataName("System.Text.Json.JsonSerializerDefaults")?
            .GetMembers("Strict")
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.HasConstantValue);

        // Both properties were introduced in .NET 9
        private bool SupportsRespectNullableAnnotations => HasBooleanProperty(_attributeSymbol, RespectNullableAnnotationsPropertyName);

        private bool SupportsRespectRequiredConstructorParameters => HasBooleanProperty(_attributeSymbol, RespectRequiredConstructorParametersPropertyName);

        public bool IsValid => _jsonSerializerContextSymbol is not null && _attributeSymbol is not null
            && (SupportsRespectNullableAnnotations || SupportsRespectRequiredConstructorParameters);

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!symbol.InheritsFrom(_jsonSerializerContextSymbol))
                return;

            var attribute = symbol.GetFirstAttribute(_attributeSymbol);
            var setByDefaults = attribute is not null && UsesStrictDefaults(attribute);

            if (SupportsRespectNullableAnnotations && !IsSet(attribute, RespectNullableAnnotationsPropertyName, setByDefaults))
            {
                Report(context, symbol, attribute, RespectNullableAnnotationsRule);
            }

            if (SupportsRespectRequiredConstructorParameters && !IsSet(attribute, RespectRequiredConstructorParametersPropertyName, setByDefaults))
            {
                Report(context, symbol, attribute, RespectRequiredConstructorParametersRule);
            }
        }

        private bool UsesStrictDefaults(AttributeData attribute)
        {
            if (_strictDefaults is null || attribute.ConstructorArguments.Length is 0)
                return false;

            var argument = attribute.ConstructorArguments[0];
            return argument.Type.IsEqualTo(_strictDefaults.ContainingType) && Equals(argument.Value, _strictDefaults.ConstantValue);
        }

        private static bool HasBooleanProperty(INamedTypeSymbol? attributeSymbol, string propertyName)
        {
            if (attributeSymbol is null)
                return false;

            foreach (var member in attributeSymbol.GetMembers(propertyName))
            {
                if (member is IPropertySymbol { Type.SpecialType: SpecialType.System_Boolean })
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the option is explicitly configured, whatever the value it is set to.
        /// </summary>
        private static bool IsSet(AttributeData? attribute, string propertyName, bool setByDefaults)
        {
            if (attribute is null)
                return false;

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (string.Equals(namedArgument.Key, propertyName, StringComparison.Ordinal))
                    return true;
            }

            return setByDefaults;
        }

        private static void Report(SymbolAnalysisContext context, INamedTypeSymbol symbol, AttributeData? attribute, DiagnosticDescriptor rule)
        {
            if (attribute is not null)
            {
                context.ReportDiagnostic(rule, attribute);
            }
            else if (GetDeclarationLocation(context, symbol) is { } location)
            {
                context.ReportDiagnostic(rule, location);
            }
        }

        /// <summary>
        /// Gets the location of the declaration the attribute must be added to. The source generator declares another
        /// part of the context, so the symbol has a location in generated code, where the attribute cannot be added.
        /// </summary>
        private static Location? GetDeclarationLocation(SymbolAnalysisContext context, INamedTypeSymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree is null || !location.SourceTree.IsGeneratedCode(context.Options, context.CancellationToken))
                    return location;
            }

            return symbol.Locations.FirstOrDefault();
        }
    }
}
