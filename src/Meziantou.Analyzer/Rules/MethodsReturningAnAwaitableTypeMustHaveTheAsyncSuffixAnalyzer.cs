using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor AsyncSuffixRule = new(
        RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix,
        title: "Use 'Async' suffix when a method returns an awaitable type",
        messageFormat: "Method returning an awaitable type must use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor NotAsyncSuffixRule = new(
        RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix,
        title: "Do not use 'Async' suffix when a method does not return an awaitable type",
        messageFormat: "Method not returning an awaitable type must not use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor AsyncSuffixRuleAsyncEnumerable = new(
       RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix,
       title: "Use 'Async' suffix when a method returns IAsyncEnumerable<T>",
       messageFormat: "Method returning IAsyncEnumerable<T> must use the 'Async' suffix",
       RuleCategories.Design,
       DiagnosticSeverity.Warning,
       isEnabledByDefault: false,
       description: "",
       helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor NotAsyncSuffixRuleAsyncEnumerable = new(
        RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix,
        title: "Do not use 'Async' suffix when a method returns IAsyncEnumerable<T>",
        messageFormat: "Method returning IAsyncEnumerable<T> must not use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix));

    private static readonly ConfigurationDefinition<bool> ExcludeTestMethodsConfiguration = new("MA0137.exclude_test_methods", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> ExcludePropertyAccessorsConfiguration = new("MA0137.exclude_property_accessors", defaultValue: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AsyncSuffixRule, NotAsyncSuffixRule, AsyncSuffixRuleAsyncEnumerable, NotAsyncSuffixRuleAsyncEnumerable);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(ctx =>
        {
            var context = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterSymbolAction(context.AnalyzeSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(context.AnalyzeLocalFunction, OperationKind.LocalFunction);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private static readonly ImmutableHashSet<string> WellKnownMethodNames = ImmutableHashSet.Create(StringComparer.Ordinal, "ConfigureAwait", "GetAwaiter", "WithCancellation");

        private readonly AwaitableTypes _awaitableTypes = new(compilation);
        private readonly INamedTypeSymbol? _iasyncEnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        private readonly INamedTypeSymbol? _benchmarkSymbol = compilation.GetBestTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.IsOverrideOrInterfaceImplementation())
                return;

            if (method.IsTopLevelStatementsEntryPointMethod())
                return;

            if (method.IsEqualTo(context.Compilation.GetEntryPoint(context.CancellationToken)))
                return;

            if (WellKnownMethodNames.Contains(method.Name))
                return;

            if (MustIgnoreSymbol(context.Options, method))
                return;

            var hasAsyncSuffix = method.Name.EndsWith("Async", StringComparison.Ordinal);
            if (_awaitableTypes.IsAwaitable(method.ReturnType))
            {
                if (!hasAsyncSuffix)
                {
                    context.ReportDiagnostic(AsyncSuffixRule, method);
                }
            }
            else if ((method.ReturnType as INamedTypeSymbol)?.ConstructedFrom.IsOrImplements(_iasyncEnumerableSymbol) is true)
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRuleAsyncEnumerable, method);
                }
                else
                {
                    context.ReportDiagnostic(AsyncSuffixRuleAsyncEnumerable, method);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRule, method);
                }
            }
        }

        public void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var operation = (ILocalFunctionOperation)context.Operation;
            var method = operation.Symbol;

            if (WellKnownMethodNames.Contains(method.Name))
                return;
            var hasAsyncSuffix = method.Name.EndsWith("Async", StringComparison.Ordinal);
            if (_awaitableTypes.IsAwaitable(method.ReturnType))
            {
                if (!hasAsyncSuffix)
                {
                    context.ReportDiagnostic(AsyncSuffixRule, properties: default, operation, DiagnosticMethodReportOptions.ReportOnMethodName);
                }
            }
            else if ((method.ReturnType as INamedTypeSymbol)?.ConstructedFrom.IsOrImplements(_iasyncEnumerableSymbol) is true)
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRuleAsyncEnumerable, method);
                }
                else
                {
                    context.ReportDiagnostic(AsyncSuffixRuleAsyncEnumerable, method);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRule, properties: default, operation, DiagnosticMethodReportOptions.ReportOnMethodName);
                }
            }
        }

        private bool MustIgnoreSymbol(AnalyzerOptions options, IMethodSymbol symbol)
        {
            if (symbol.HasAttribute(_benchmarkSymbol))
                return true;

            var excludeTestMethods = options.GetConfigurationValue(symbol, ExcludeTestMethodsConfiguration);
            if (excludeTestMethods && symbol.IsUnitTestMethod())
                return true;

            if (symbol.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove)
            {
                var excludePropertyAccessors = options.GetConfigurationValue(symbol, ExcludePropertyAccessorsConfiguration);
                if (excludePropertyAccessors)
                    return true;
            }

            return false;
        }
    }
}
