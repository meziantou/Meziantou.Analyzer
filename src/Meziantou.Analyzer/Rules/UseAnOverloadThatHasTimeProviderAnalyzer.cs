using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAnOverloadThatHasTimeProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UseAnOverloadThatHasTimeProviderRule = new(
        RuleIdentifiers.UseAnOverloadThatHasTimeProvider,
        title: "Use an overload with a TimeProvider argument",
        messageFormat: "Use an overload with a TimeProvider",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasTimeProvider));

    private static readonly DiagnosticDescriptor UseAnOverloadThatHasTimeProviderWhenAvailable = new(
        RuleIdentifiers.UseAnOverloadThatHasTimeProviderWhenAvailable,
        title: "Forward the TimeProvider to methods that take one",
        messageFormat: "Use an overload with a TimeProvider, available time providers: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasTimeProviderWhenAvailable));

    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.UseAnOverloadThatHasTimeProvider + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesWhenAvailableConfiguration = new(RuleIdentifiers.UseAnOverloadThatHasTimeProviderWhenAvailable + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(UseAnOverloadThatHasTimeProviderRule, UseAnOverloadThatHasTimeProviderWhenAvailable);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.TimeProviderSymbol is null)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly AvailableValueFinder _timeProviderFinder = CreateTimeProviderFinder(compilation);

        public INamedTypeSymbol TimeProviderSymbol { get; } = compilation.GetTypeByMetadataName("System.TimeProvider")!; // not null as we check it in the constructor

        private static AvailableValueFinder CreateTimeProviderFinder(Compilation compilation)
        {
            var timeProviderSymbol = compilation.GetTypeByMetadataName("System.TimeProvider");

            // A type that derives from TimeProvider can be passed as a TimeProvider
            return new AvailableValueFinder(isSearchedType: type => type.IsOrInheritsFrom(timeProviderSymbol));
        }

        private bool HasExplicitTimeProviderArgument(IInvocationOperation operation)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind == ArgumentKind.Explicit && argument.Parameter is not null && argument.Parameter.Type.IsEqualTo(TimeProviderSymbol))
                    return true;
            }

            return false;
        }

        /// <param name="NamespaceToImport">The namespace to import to call the overload, when it is an extension method declared in a namespace that is not imported.</param>
        private sealed record AdditionalParameterInfo(int ParameterIndex, string? Name, string? NamespaceToImport = null);

        private bool HasAnOverloadWithTimeProvider(OperationAnalysisContext context, IInvocationOperation operation, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
        {
            if (IsArgumentImplicitlyDeclared(operation, TimeProviderSymbol, out parameterInfo))
                return true;

            var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration)
                || context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesWhenAvailableConfiguration);
            var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces), [TimeProviderSymbol]);
            if (overload is not null)
            {
                var namespaceToImport = includeExtensionMethodsFromNotImportedNamespaces ? _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) : null;
                parameterInfo = null;
                for (var i = 0; i < overload.Parameters.Length; i++)
                {
                    if (overload.Parameters[i].Type.IsEqualTo(TimeProviderSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(i, overload.Parameters[i].Name, namespaceToImport);
                        break;
                    }
                }

                return parameterInfo is not null;
            }

            return false;

            static bool IsArgumentImplicitlyDeclared(IInvocationOperation invocationOperation, INamedTypeSymbol timeProviderSymbol, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
            {
                foreach (var arg in invocationOperation.Arguments)
                {
                    if (arg.ArgumentKind is ArgumentKind.DefaultValue && arg.Parameter is not null && arg.Parameter.Type.IsEqualTo(timeProviderSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(invocationOperation.TargetMethod.Parameters.IndexOf(arg.Parameter), arg.Parameter.Name);
                        return true;
                    }
                }

                parameterInfo = null;
                return false;
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (HasExplicitTimeProviderArgument(operation))
                return;

            if (!HasAnOverloadWithTimeProvider(context, operation, out var parameterInfo))
                return;

            var availableTimeProviders = _timeProviderFinder.FindPaths(operation, context.CancellationToken);

            // An extension method declared in a namespace that is not imported is only included when the reported rule is configured to
            if (parameterInfo.NamespaceToImport is not null)
            {
                var configuration = availableTimeProviders.Length > 0 ? IncludeExtensionMethodsFromNotImportedNamespacesWhenAvailableConfiguration : IncludeExtensionMethodsFromNotImportedNamespacesConfiguration;
                if (!context.Options.GetConfigurationValue(operation, configuration))
                    return;
            }
            if (availableTimeProviders.Length > 0)
            {
                context.ReportDiagnostic(UseAnOverloadThatHasTimeProviderWhenAvailable, CreateProperties(availableTimeProviders, parameterInfo), operation, string.Join(", ", availableTimeProviders));
            }
            else
            {
                var parentMethod = operation.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(UseAnOverloadThatHasTimeProviderRule, CreateProperties(availableTimeProviders, parameterInfo), operation);
            }
        }

        private static ImmutableDictionary<string, string?> CreateProperties(string[] timeProviders, AdditionalParameterInfo parameterInfo)
        {
            var properties = ImmutableDictionary.Create<string, string?>(StringComparer.Ordinal)
                .Add(UseAnOverloadThatHasTimeProviderAnalyzerCommon.ParameterIndexKey, parameterInfo.ParameterIndex.ToString(CultureInfo.InvariantCulture))
                .Add(UseAnOverloadThatHasTimeProviderAnalyzerCommon.ParameterNameKey, parameterInfo.Name)
                .Add(UseAnOverloadThatHasTimeProviderAnalyzerCommon.PathsKey, string.Join(',', timeProviders));

            if (parameterInfo.NamespaceToImport is not null)
            {
                properties = properties.Add(OverloadFinder.NamespaceToImportPropertyName, parameterInfo.NamespaceToImport);
            }

            return properties;
        }
    }
}
