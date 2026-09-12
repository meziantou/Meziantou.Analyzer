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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext = new(compilation);
        private readonly OverloadFinder _overloadFinder = new(compilation);

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation is null)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            var options = GetOptions(context, operation);
            if (!CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, options), options))
                return;

            var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration);

            // The overloads with an additional styles or format parameter are searched with the default options of OverloadFinder
            var stylesOverloadOptions = new OverloadOptions { IncludeExtensionMethodsFromNotImportedNamespaces = includeExtensionMethodsFromNotImportedNamespaces };
            if (_cultureSensitiveContext.FormatProviderSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.FormatProviderSymbol))
            {
                if (operation.TargetMethod.Name == "ToString" && operation.Arguments.Length == 0 && operation.TargetMethod.ContainingType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces), [_cultureSensitiveContext.FormatProviderSymbol]);
                if (overload is not null)
                {
                    if (CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, GetOptions(context, operation, unwrapNullableTypes: false)), options))
                    {
                        context.ReportDiagnostic(Rule, CreateProperties(operation, overload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    }

                    return;
                }

                var targetMethodType = operation.TargetMethod.ContainingType;
                if (targetMethodType.IsNumberType() && _cultureSensitiveContext.NumberStyleSymbol is not null && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.NumberStyleSymbol]) is { } numberStyleOverload)
                {
                    context.ReportDiagnostic(Rule, CreateProperties(operation, numberStyleOverload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var isDateTime = targetMethodType.IsDateTime() || targetMethodType.IsEqualToAny(_cultureSensitiveContext.DateTimeOffsetSymbol, _cultureSensitiveContext.DateOnlySymbol, _cultureSensitiveContext.TimeOnlySymbol);
                if (isDateTime)
                {
                    if (_cultureSensitiveContext.DateTimeStyleSymbol is not null && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.DateTimeStyleSymbol]) is { } dateTimeStyleOverload)
                    {
                        context.ReportDiagnostic(Rule, CreateProperties(operation, dateTimeStyleOverload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                        return;
                    }
                }

                if (operation.Arguments.IsEmpty && targetMethodType.Implements(_cultureSensitiveContext.SystemIFormattableSymbol) && _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, stylesOverloadOptions, [_cultureSensitiveContext.FormatProviderSymbol, compilation.GetSpecialType(SpecialType.System_String)]) is { } formatOverload)
                {
                    context.ReportDiagnostic(Rule, CreateProperties(operation, formatOverload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }
            }

            if (_cultureSensitiveContext.CultureInfoSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.CultureInfoSymbol))
            {
                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces), [_cultureSensitiveContext.CultureInfoSymbol]);
                if (overload is not null)
                {
                    if (CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, GetOptions(context, operation, unwrapNullableTypes: false)), options))
                    {
                        context.ReportDiagnostic(Rule, CreateProperties(operation, overload, includeExtensionMethodsFromNotImportedNamespaces), operation, operation.TargetMethod.Name, _cultureSensitiveContext.CultureInfoSymbol.ToDisplayString());
                    }

                    return;
                }
            }
        }

        private ImmutableDictionary<string, string?> CreateProperties(IInvocationOperation operation, IMethodSymbol overload, bool includeExtensionMethodsFromNotImportedNamespaces)
        {
            if (includeExtensionMethodsFromNotImportedNamespaces && _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) is { } namespaceToImport)
                return ImmutableDictionary<string, string?>.Empty.Add(OverloadFinder.NamespaceToImportPropertyName, namespaceToImport);

            return ImmutableDictionary<string, string?>.Empty;
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name.EndsWith("OrDefault", StringComparison.Ordinal))
                return true;

            // ToString show culture-sensitive data by default
            if (operation.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, ExcludeToStringMethodsConfiguration);
            }

            return false;
        }

        private static CultureSensitiveOptions GetOptions(OperationAnalysisContext context, IOperation operation, bool? unwrapNullableTypes = null)
        {
            var options = CultureSensitiveOptions.None;
            var syntaxTree = operation.Syntax.SyntaxTree;

            if (unwrapNullableTypes ?? context.Options.GetConfigurationValue(syntaxTree, ConsiderNullableTypesConfiguration))
                options |= CultureSensitiveOptions.UnwrapNullableOfT;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatUnsealedTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

            return options;
        }
    }
}
