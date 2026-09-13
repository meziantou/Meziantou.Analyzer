using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringComparisonAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor AvoidCultureSensitiveMethodRule = new(
        RuleIdentifiers.AvoidCultureSensitiveMethod,
        title: "Avoid implicit culture-sensitive methods",
        messageFormat: "Use an overload of '{0}' that has a StringComparison parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidCultureSensitiveMethod));

    private static readonly DiagnosticDescriptor UseStringComparisonRule = new(
        RuleIdentifiers.UseStringComparison,
        title: "StringComparison is missing",
        messageFormat: "Use an overload of '{0}' that has a StringComparison parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparison));

    private static readonly ConfigurationDefinition<bool> AvoidCultureSensitiveMethodIncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.AvoidCultureSensitiveMethod + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> UseStringComparisonIncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(RuleIdentifiers.UseStringComparison + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AvoidCultureSensitiveMethodRule, UseStringComparisonRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (!analyzerContext.IsValid)
                return;

            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly OperationUtilities _operationUtilities = new(compilation);
        private readonly INamedTypeSymbol _stringComparisonSymbol = compilation.GetTypeByMetadataName("System.StringComparison")!;
        private readonly INamedTypeSymbol? _jobjectSymbol = compilation.GetTypeByMetadataName("Newtonsoft.Json.Linq.JObject");
        private readonly INamedTypeSymbol? _xunitAssertSymbol = compilation.GetTypeByMetadataName("XUnit.Assert");
        private readonly INamedTypeSymbol? _meziantouFrameworkAssertSymbol = compilation.GetTypeByMetadataName("Meziantou.Framework.Assertions.Assert");
        private readonly HashSet<ISymbol> _nonCultureSensitiveSymbols = CreateNonCultureSensitiveSymbols(compilation);

        public bool IsValid => _stringComparisonSymbol is not null;

        private static HashSet<ISymbol> CreateNonCultureSensitiveSymbols(Compilation compilation)
        {
            var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            Add("M:Microsoft.Extensions.Primitives.StringSegment.Equals(Microsoft.Extensions.Primitives.StringSegment)~System.Boolean");
            Add("M:Microsoft.Extensions.Primitives.StringSegment.Equals(System.String)~System.Boolean");
            Add("M:System.Char.Equals(System.Char)~System.Boolean");
            Add("M:System.IO.Path.GetRelativePath(System.String,System.String)~System.String");
            Add("M:System.MemoryExtensions.EndsWith``1(System.ReadOnlySpan{``0},System.ReadOnlySpan{``0})~System.Boolean");
            Add("M:System.MemoryExtensions.EndsWith``1(System.ReadOnlySpan{``0},``0)~System.Boolean");
            Add("M:System.Security.Claims.ClaimsIdentity.#ctor(System.IO.BinaryReader)");
            Add("M:System.Security.Claims.ClaimsIdentity.#ctor(System.Security.Claims.ClaimsIdentity)");
            Add("M:System.Security.Claims.ClaimsIdentity.#ctor(System.Security.Principal.IIdentity,System.Collections.Generic.IEnumerable{System.Security.Claims.Claim},System.String,System.String,System.String)");
            Add("M:System.String.Contains(System.Char)~System.Boolean");
            Add("M:System.String.Contains(System.Text.Rune)~System.Boolean");
            Add("M:System.String.EndsWith(System.Char)~System.Boolean");
            Add("M:System.String.EndsWith(System.Text.Rune)~System.Boolean");
            Add("M:System.String.Equals(System.String)~System.Boolean");
            Add("M:System.String.Equals(System.String,System.String)~System.Boolean");
            Add("M:System.String.GetHashCode(System.ReadOnlySpan{System.Char})~System.Int32");
            Add("M:System.String.GetHashCode~System.Int32");
            Add("M:System.String.IndexOf(System.Char)~System.Int32");
            Add("M:System.String.IndexOf(System.Char,System.Int32)~System.Int32");
            Add("M:System.String.IndexOf(System.Char,System.Int32,System.Int32)~System.Int32");
            Add("M:System.String.IndexOf(System.Text.Rune)~System.Int32");
            Add("M:System.String.IndexOf(System.Text.Rune,System.Int32)~System.Int32");
            Add("M:System.String.IndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Char)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Char,System.Int32)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Char,System.Int32,System.Int32)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Text.Rune)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Text.Rune,System.Int32)~System.Int32");
            Add("M:System.String.LastIndexOf(System.Text.Rune,System.Int32,System.Int32)~System.Int32");
            Add("M:System.String.Replace(System.String,System.String)~System.String");
            Add("M:System.String.StartsWith(System.Char)~System.Boolean");
            Add("M:System.String.StartsWith(System.Text.Rune)~System.Boolean");
            Add("M:System.Text.Rune.Equals(System.Text.Rune)~System.Boolean");
            return symbols;

            void Add(string documentationId)
            {
                foreach (var symbol in DocumentationCommentId.GetSymbolsForDeclarationId(documentationId, compilation))
                {
                    symbols.Add(symbol);
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (!operation.HasArgumentOfType(_stringComparisonSymbol))
            {
                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (_operationUtilities.IsInExpressionContext(operation))
                    return;

                // Check if there is an overload with a StringComparison
                var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, UseStringComparisonIncludeExtensionMethodsFromNotImportedNamespacesConfiguration)
                    || context.Options.GetConfigurationValue(operation, AvoidCultureSensitiveMethodIncludeExtensionMethodsFromNotImportedNamespacesConfiguration);
                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions { IncludeExtensionMethodsFromNotImportedNamespaces = includeExtensionMethodsFromNotImportedNamespaces }, [_stringComparisonSymbol]);
                if (overload is not null)
                {
                    var (rule, configuration) = IsNonCultureSensitiveMethod(operation)
                        ? (UseStringComparisonRule, UseStringComparisonIncludeExtensionMethodsFromNotImportedNamespacesConfiguration)
                        : (AvoidCultureSensitiveMethodRule, AvoidCultureSensitiveMethodIncludeExtensionMethodsFromNotImportedNamespacesConfiguration);

                    // An extension method declared in a namespace that is not imported is only included when the reported rule is configured to
                    var properties = ImmutableDictionary<string, string?>.Empty;
                    if (includeExtensionMethodsFromNotImportedNamespaces && _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) is { } namespaceToImport)
                    {
                        if (!context.Options.GetConfigurationValue(operation, configuration))
                            return;

                        properties = properties.Add(OverloadFinder.NamespaceToImportPropertyName, namespaceToImport);
                    }

                    context.ReportDiagnostic(rule, properties, operation, operation.TargetMethod.Name);
                }
            }
        }

        private bool IsNonCultureSensitiveMethod(IInvocationOperation operation)
        {
            var method = operation.TargetMethod;
            if (method is null)
                return false;

            if (_nonCultureSensitiveSymbols.Contains(method))
                return true;

            // JObject.Property / TryGetValue / GetValue
            if (method.ContainingType.IsEqualTo(_jobjectSymbol))
                return true;

            // Xunit.Assert.Contains/NotContains
            if (method.ContainingType.IsEqualTo(_xunitAssertSymbol))
                return true;

            // Meziantou.Framework.Assertions.Assert
            if (method.ContainingType.IsEqualTo(_meziantouFrameworkAssertSymbol))
                return true;

            return false;
        }
    }
}
