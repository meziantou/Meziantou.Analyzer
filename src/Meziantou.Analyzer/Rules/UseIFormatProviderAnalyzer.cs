using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseIFormatProviderParameter,
        title: "IFormatProvider is missing",
        messageFormat: "Use an overload of '{0}' that has a '{1}' parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

    private static readonly ConfigurationDefinition<bool> ExcludeToStringMethodsConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".exclude_tostring_methods", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> ConsiderNullableTypesConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".consider_nullable_types", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".treat_opaque_runtime_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> TreatUnsealedTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".treat_unsealed_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterSymbolStartAction(symbolContext =>
            {
                var lookupCache = new OverloadLookupCache();
                symbolContext.RegisterOperationAction(operationContext => analyzerContext.AnalyzeInvocation(operationContext, lookupCache), OperationKind.Invocation);
            }, SymbolKind.NamedType);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext = new(compilation);
        private readonly OverloadFinder _overloadFinder = new(compilation);

        public void AnalyzeInvocation(OperationAnalysisContext context, OverloadLookupCache lookupCache)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.TargetMethod.Name.EndsWith("OrDefault", StringComparison.Ordinal))
                return;

            // Most methods have no overload to report, and finding it is cheaper than computing the culture sensitivity of the invocation
            var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration);
            if (FindOverloadToReport(operation, lookupCache, includeExtensionMethodsFromNotImportedNamespaces) is not { } overloadToReport)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            var options = GetOptions(context);
            if (!CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, options), options))
                return;

            // The culture sensitivity without unwrapping the nullable types is the same when they are not unwrapped
            if (overloadToReport.MustBeCultureSensitiveWithoutUnwrappingNullableTypes && (options & CultureSensitiveOptions.UnwrapNullableOfT) == CultureSensitiveOptions.UnwrapNullableOfT)
            {
                if (!CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, options & ~CultureSensitiveOptions.UnwrapNullableOfT), options))
                    return;
            }

            context.ReportDiagnostic(Rule, CreateProperties(operation, overloadToReport.Overload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, overloadToReport.ParameterType.ToDisplayString());
        }

        /// <summary>
        /// Finds the overload to suggest when the invocation is culture sensitive. It does not depend on the culture sensitivity of the
        /// invocation, so it is done first to skip computing the culture sensitivity of most invocations.
        /// </summary>
        private OverloadToReport? FindOverloadToReport(IInvocationOperation operation, OverloadLookupCache lookupCache, bool includeExtensionMethodsFromNotImportedNamespaces)
        {
            // The overloads with an additional styles or format parameter are searched with the default options of OverloadFinder
            var stylesOverloadOptions = new OverloadOptions { IncludeExtensionMethodsFromNotImportedNamespaces = includeExtensionMethodsFromNotImportedNamespaces, LookupCache = lookupCache };
            if (_cultureSensitiveContext.FormatProviderSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.FormatProviderSymbol))
            {
                if (operation.TargetMethod.Name == "ToString" && operation.Arguments.Length == 0 && operation.TargetMethod.ContainingType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                    return new OverloadToReport(Overload: null, _cultureSensitiveContext.FormatProviderSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: false);

                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces, LookupCache: lookupCache), _cultureSensitiveContext.FormatProviderSymbol);
                if (overload is not null)
                    return new OverloadToReport(overload, _cultureSensitiveContext.FormatProviderSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: true);

                var targetMethodType = operation.TargetMethod.ContainingType;
                if (targetMethodType.IsNumberType() && _cultureSensitiveContext.NumberStyleSymbol is not null && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.NumberStyleSymbol]) is { } numberStyleOverload)
                    return new OverloadToReport(numberStyleOverload, _cultureSensitiveContext.FormatProviderSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: false);

                var isDateTime = targetMethodType.IsDateTime() || targetMethodType.IsEqualToAny(_cultureSensitiveContext.DateTimeOffsetSymbol, _cultureSensitiveContext.DateOnlySymbol, _cultureSensitiveContext.TimeOnlySymbol);
                if (isDateTime)
                {
                    if (_cultureSensitiveContext.DateTimeStyleSymbol is not null && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.DateTimeStyleSymbol]) is { } dateTimeStyleOverload)
                        return new OverloadToReport(dateTimeStyleOverload, _cultureSensitiveContext.FormatProviderSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: false);
                }

                if (operation.Arguments.IsEmpty && targetMethodType.Implements(_cultureSensitiveContext.SystemIFormattableSymbol) && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, compilation.GetSpecialType(SpecialType.System_String)]) is { } formatOverload)
                    return new OverloadToReport(formatOverload, _cultureSensitiveContext.FormatProviderSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: false);
            }

            if (_cultureSensitiveContext.CultureInfoSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.CultureInfoSymbol))
            {
                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces, LookupCache: lookupCache), _cultureSensitiveContext.CultureInfoSymbol);
                if (overload is not null)
                    return new OverloadToReport(overload, _cultureSensitiveContext.CultureInfoSymbol, MustBeCultureSensitiveWithoutUnwrappingNullableTypes: true);
            }

            return null;
        }

        private ImmutableDictionary<string, string?> CreateProperties(IInvocationOperation operation, IMethodSymbol? overload, bool includeExtensionMethodsFromNotImportedNamespaces)
        {
            if (overload is not null && includeExtensionMethodsFromNotImportedNamespaces && _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) is { } namespaceToImport)
                return ImmutableDictionary<string, string?>.Empty.Add(OverloadFinder.NamespaceToImportPropertyName, namespaceToImport);

            return ImmutableDictionary<string, string?>.Empty;
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            // ToString show culture-sensitive data by default
            if (operation.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, ExcludeToStringMethodsConfiguration);
            }

            return false;
        }

        private static CultureSensitiveOptions GetOptions(OperationAnalysisContext context)
        {
            var options = CultureSensitiveOptions.None;
            var syntaxTree = context.Operation.Syntax.SyntaxTree;

            if (context.Options.GetConfigurationValue(syntaxTree, ConsiderNullableTypesConfiguration))
                options |= CultureSensitiveOptions.UnwrapNullableOfT;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatUnsealedTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

            return options;
        }

        /// <summary>The overload to suggest, and the type of its additional parameter.</summary>
        private readonly record struct OverloadToReport(IMethodSymbol? Overload, INamedTypeSymbol ParameterType, bool MustBeCultureSensitiveWithoutUnwrappingNullableTypes);
    }
}
