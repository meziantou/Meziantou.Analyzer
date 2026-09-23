using System.Collections.Concurrent;
using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAnOverloadThatHasCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UseAnOverloadThatHasCancellationTokenRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationToken,
        title: "Use an overload with a CancellationToken argument, even when no token is available in scope",
        messageFormat: "Use an overload with a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationToken));

    private static readonly DiagnosticDescriptor UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken parameter to methods that take one",
        messageFormat: "Use an overload with a CancellationToken, available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable));

    private static readonly DiagnosticDescriptor FlowCancellationTokenInAwaitForEachRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEach,
        title: "Use a cancellation token using .WithCancellation()",
        messageFormat: "Specify a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEach));

    private static readonly DiagnosticDescriptor FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken using .WithCancellation()",
        messageFormat: "Specify a CancellationToken using WithCancellation(), available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable));

    private static readonly ConfigurationDefinition<bool> AllowOverloadsWithOptionalParametersConfiguration = new(["MA0032.allow_overloads_with_optional_parameters", "MA0032.allowOverloadsWithOptionalParameters"], defaultValue: false);
    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.UseAnOverloadThatHasCancellationToken + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesWhenACancellationTokenIsAvailableConfiguration = new(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(UseAnOverloadThatHasCancellationTokenRule, UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, FlowCancellationTokenInAwaitForEachRule, FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.CancellationTokenSymbol is null)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeLoop, OperationKind.Loop);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentDictionary<ISymbol, bool> _excludedMethods = new(SymbolEqualityComparer.Default);

        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly HashSet<ISymbol> _excludedSymbols = AnnotationExclusions.GetExcludedSymbols(compilation, AnnotationAttributes.IsExcludeFromCancellationTokenAnalysisAttributeSymbol);
        private readonly AvailableValueFinder _cancellationTokenFinder = CreateCancellationTokenFinder(compilation);

        public INamedTypeSymbol CancellationTokenSymbol { get; } = compilation.GetTypeByMetadataName("System.Threading.CancellationToken")!;  // Not nullable as it is checked before registering the Operation actions
        public INamedTypeSymbol? CancellationTokenSourceSymbol { get; } = compilation.GetTypeByMetadataName("System.Threading.CancellationTokenSource");
        private INamedTypeSymbol? ConfiguredCancelableAsyncEnumerableSymbol { get; } = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1");
        private INamedTypeSymbol? EnumeratorCancellationAttributeSymbol { get; } = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.EnumeratorCancellationAttribute");

        private static AvailableValueFinder CreateCancellationTokenFinder(Compilation compilation)
        {
            var cancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            var taskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var xunitTestContextSymbol = compilation.GetTypeByMetadataName("Xunit.TestContext");
            return new AvailableValueFinder(
                isSearchedType: type => type.IsEqualTo(cancellationTokenSymbol),
                isIgnoredType: type => type.IsEqualTo(taskSymbol) || type.OriginalDefinition.IsEqualTo(taskOfTSymbol),
                additionalPaths: xunitTestContextSymbol is not null ? ImmutableArray.Create("Xunit.TestContext.Current.CancellationToken") : default);
        }

        private bool IsExcluded(IMethodSymbol method)
        {
            if (_excludedMethods.TryGetValue(method, out var isExcluded))
                return isExcluded;

            isExcluded = ComputeIsExcluded(method);
            _excludedMethods[method] = isExcluded;
            return isExcluded;
        }

        private bool ComputeIsExcluded(IMethodSymbol method)
        {
            var definition = (method.ReducedFrom ?? method).OriginalDefinition;
            if (_excludedSymbols.Count > 0 && (_excludedSymbols.Contains(method) || _excludedSymbols.Contains(definition)))
                return true;

            foreach (var attribute in definition.GetAttributes())
            {
                if (AnnotationAttributes.IsExcludeFromCancellationTokenAnalysisAttributeSymbol(attribute.AttributeClass))
                    return true;
            }

            return false;
        }

        private bool HasExplicitCancellationTokenArgument(IInvocationOperation operation)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind == ArgumentKind.Explicit && argument.Parameter is not null && argument.Parameter.Type.IsEqualTo(CancellationTokenSymbol))
                    return true;
            }

            return false;
        }

        /// <param name="NamespaceToImport">The namespace to import to call the overload, when it is an extension method declared in a namespace that is not imported.</param>
        private sealed record AdditionalParameterInfo(int ParameterIndex, string? Name, bool HasEnumeratorCancellationAttribute, string? NamespaceToImport = null);

        private bool HasAnOverloadWithCancellationToken(OperationAnalysisContext context, IInvocationOperation operation, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
        {
            parameterInfo = default;
            var method = operation.TargetMethod;
            if (method.Name == nameof(CancellationTokenSource.CreateLinkedTokenSource) && method.ContainingType.IsEqualTo(CancellationTokenSourceSymbol))
                return false;

            if (IsArgumentImplicitlyDeclared(operation, CancellationTokenSymbol, out parameterInfo))
                return true;

            var allowOptionalParameters = context.Options.GetConfigurationValue(operation, AllowOverloadsWithOptionalParametersConfiguration);
            var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration)
                || context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesWhenACancellationTokenIsAvailableConfiguration);
            var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(AllowOptionalParameters: allowOptionalParameters, IncludeExtensionMethodsFromNotImportedNamespaces: includeExtensionMethodsFromNotImportedNamespaces), [CancellationTokenSymbol]);
            if (overload is not null)
            {
                var namespaceToImport = includeExtensionMethodsFromNotImportedNamespaces ? _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) : null;
                parameterInfo = null;
                for (var i = 0; i < overload.Parameters.Length; i++)
                {
                    if (overload.Parameters[i].Type.IsEqualTo(CancellationTokenSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(i, overload.Parameters[i].Name, HasEnumerableCancellationAttribute(overload.Parameters[i]), namespaceToImport);
                        break;
                    }
                }

                return parameterInfo is not null;
            }

            return false;

            bool IsArgumentImplicitlyDeclared(IInvocationOperation invocationOperation, INamedTypeSymbol cancellationTokenSymbol, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
            {
                foreach (var arg in invocationOperation.Arguments)
                {
                    if (arg.ArgumentKind is ArgumentKind.DefaultValue && arg.Parameter is not null && arg.Parameter.Type.IsEqualTo(cancellationTokenSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(invocationOperation.TargetMethod.Parameters.IndexOf(arg.Parameter), arg.Parameter.Name, HasEnumerableCancellationAttribute(arg.Parameter));
                        return true;
                    }
                }

                parameterInfo = null;
                return false;
            }
        }

        private bool HasEnumerableCancellationAttribute(IParameterSymbol? parameterSymbol)
        {
            if (parameterSymbol is null)
                return false;

            return parameterSymbol.HasAttribute(EnumeratorCancellationAttributeSymbol, inherits: false);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (HasExplicitCancellationTokenArgument(operation))
                return;

            if (!HasAnOverloadWithCancellationToken(context, operation, out var parameterInfo))
                return;

            if (IsExcluded(operation.TargetMethod))
                return;

            var availableCancellationTokens = _cancellationTokenFinder.FindPaths(operation, context.CancellationToken);
            if (!IsOverloadIncluded(context, operation, parameterInfo, hasAvailableCancellationTokens: availableCancellationTokens.Length > 0))
                return;

            if (availableCancellationTokens.Length > 0)
            {
                context.ReportDiagnostic(UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, CreateProperties(availableCancellationTokens, parameterInfo), operation, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = operation.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(UseAnOverloadThatHasCancellationTokenRule, CreateProperties(availableCancellationTokens, parameterInfo), operation);
            }
        }

        /// <summary>
        /// Indicates whether the rule reported for the invocation, which depends on the availability of a cancellation token, includes
        /// the overload. An extension method declared in a namespace that is not imported is only included when the rule is configured to.
        /// </summary>
        private static bool IsOverloadIncluded(OperationAnalysisContext context, IOperation operation, AdditionalParameterInfo parameterInfo, bool hasAvailableCancellationTokens)
        {
            if (parameterInfo.NamespaceToImport is null)
                return true;

            var configuration = hasAvailableCancellationTokens ? IncludeExtensionMethodsFromNotImportedNamespacesWhenACancellationTokenIsAvailableConfiguration : IncludeExtensionMethodsFromNotImportedNamespacesConfiguration;
            return context.Options.GetConfigurationValue(operation, configuration);
        }

        public void AnalyzeLoop(OperationAnalysisContext context)
        {
            if (context.Operation is not IForEachLoopOperation op)
                return;

            if (!op.IsAsynchronous)
                return;

            var collectionType = op.Collection.GetActualType(context.CancellationToken);
            if (collectionType.IsEqualTo(ConfiguredCancelableAsyncEnumerableSymbol))
                return;

            // await foreach (var item in A(cancellationToken)) OK
            // await foreach (var item in A())                  KO
            // await foreach (var item in a)                    KO
            var collection = op.Collection;
            if (collection is IConversionOperation conversion)
            {
                collection = conversion.Operand;
            }

            while (collection is IInvocationOperation invocation)
            {
                if (IsExcluded(invocation.TargetMethod))
                    return;

                if (HasExplicitCancellationTokenArgument(invocation))
                    return;

                // Already handled by AnalyzeInvocation
                if (HasAnOverloadWithCancellationToken(context, invocation, out var invocationParameterInfo) &&
                    (invocationParameterInfo.NamespaceToImport is null || IsOverloadIncluded(context, invocation, invocationParameterInfo, hasAvailableCancellationTokens: _cancellationTokenFinder.FindPaths(invocation, context.CancellationToken).Length > 0)))
                {
                    return;
                }

                collection = invocation.GetChildOperations().FirstOrDefault();
                if (collection is IArgumentOperation argOperation)
                {
                    collection = argOperation.Value;
                }
            }

            var availableCancellationTokens = _cancellationTokenFinder.FindPaths(op, context.CancellationToken);
            if (availableCancellationTokens.Length > 0)
            {
                var properties = CreateProperties(availableCancellationTokens, new AdditionalParameterInfo(-1, Name: null, HasEnumeratorCancellationAttribute: false));
                context.ReportDiagnostic(FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule, properties, op.Collection, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = op.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(FlowCancellationTokenInAwaitForEachRule, op.Collection);
            }
        }

        private static ImmutableDictionary<string, string?> CreateProperties(string[] cancellationTokens, AdditionalParameterInfo parameterInfo)
        {
            var properties = ImmutableDictionary.Create<string, string?>(StringComparer.Ordinal)
                .Add(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterIndexKey, parameterInfo.ParameterIndex.ToString(CultureInfo.InvariantCulture))
                .Add(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterNameKey, parameterInfo.Name)
                .Add(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.ParameterIsEnumeratorCancellationKey, parameterInfo.HasEnumeratorCancellationAttribute.ToString())
                .Add(UseAnOverloadThatHasCancellationTokenAnalyzerCommon.CancellationTokensKey, string.Join(',', cancellationTokens));

            if (parameterInfo.NamespaceToImport is not null)
            {
                properties = properties.Add(OverloadFinder.NamespaceToImportPropertyName, parameterInfo.NamespaceToImport);
            }

            return properties;
        }
    }
}
