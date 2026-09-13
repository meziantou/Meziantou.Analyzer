using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringComparerAnalyzer : DiagnosticAnalyzer
{
    private const DiagnosticInvocationReportOptions DefaultDiagnosticInvocationReportOptions = DiagnosticInvocationReportOptions.ReportOnMember | DiagnosticInvocationReportOptions.ReportOnArguments;

    private static readonly string[] EnumerableMethods =
    [
        "Contains",
        "Distinct",
        "Except",
        "Intersect",
        "Order",
        "OrderBy",
        "OrderByDescending",
        "SequenceEqual",
        "ThenBy",
        "ThenByDescending",
        "ToHashSet",
        "Union",
    ];

    private static readonly Dictionary<string, int> ArityIndex = new(StringComparer.Ordinal)
    {
        { "GroupBy", 1 },
        { "GroupJoin", 2 },
        { "Join", 2 },
        { "OrderBy", 1 },
        { "OrderByDescending", 1 },
        { "ThenBy", 1 },
        { "ThenByDescending", 1 },
        { "ToDictionary", 1 },
        { "ToLookup", 1 },
    };

    // Methods whose default string comparison is ordinal (equality-based). Ordering methods
    // (Order/OrderBy/OrderByDescending/ThenBy/ThenByDescending) are intentionally excluded because
    // their default is Comparer<string>.Default (= StringComparer.CurrentCulture, culture-sensitive).
    private static readonly HashSet<string> KnownOrdinalMethodNames = new(StringComparer.Ordinal)
    {
        // System.Linq.Enumerable / System.Linq.Queryable (methods taking IEqualityComparer<string>).
        // Ordering methods (Order/OrderBy/OrderByDescending/OrderDescending/ThenBy/ThenByDescending) and
        // Min/Max/MinBy/MaxBy take IComparer<string> (culture-sensitive) and are intentionally excluded.
        "AggregateBy",
        "Contains",
        "CountBy",
        "Distinct",
        "DistinctBy",
        "Except",
        "ExceptBy",
        "GroupBy",
        "GroupJoin",
        "Intersect",
        "IntersectBy",
        "Join",
        "LeftJoin",
        "RightJoin",
        "SequenceEqual",
        "ToDictionary",
        "ToHashSet",
        "ToLookup",
        "Union",
        "UnionBy",

        // System.Collections.Immutable / System.Collections.Frozen factory methods. These names also
        // exist on the Sorted variants (IComparer<string>, culture-sensitive), but those are excluded
        // by scoping to the containers in BuildKnownOrdinalContainerTypes.
        "Create",
        "CreateBuilder",
        "CreateRange",
        "CreateRangeWithOverwrite",
        "ToImmutableDictionary",
        "ToImmutableHashSet",
        "ToFrozenDictionary",
        "ToFrozenSet",
    };

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStringComparer,
        title: "IEqualityComparer<string> or IComparer<string> is missing",
        messageFormat: "Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparer));

    private static readonly ConfigurationDefinition<bool> ExcludeQueryOperatorSyntaxesConfiguration = new(Rule.Id + ".exclude_query_operator_syntaxes", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> ReportOnlyNonOrdinalConfiguration = new(Rule.Id + ".report_only_non_ordinal", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> IncludeExtensionMethodsFromNotImportedNamespacesConfiguration = new(Rule.Id + ".include_extension_methods_from_not_imported_namespaces", defaultValue: false);
#if ROSLYN_4_14_OR_GREATER
    private static readonly ConfigurationDefinition<bool> ReportCollectionExpressionsConfiguration = new(Rule.Id + ".report_collection_expressions", defaultValue: false);
#endif

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConstructor, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
#if ROSLYN_4_14_OR_GREATER
            ctx.RegisterOperationAction(analyzerContext.AnalyzeCollectionExpression, OperationKind.CollectionExpression);
#endif
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly OperationUtilities _operationUtilities = new(compilation);

        // Types whose default string comparison is ordinal (equality-based collections). Ordering
        // collections (SortedDictionary/SortedList/SortedSet) are intentionally excluded because their
        // default is Comparer<string>.Default (= StringComparer.CurrentCulture, culture-sensitive).
        private readonly HashSet<INamedTypeSymbol> _knownOrdinalTypes = BuildKnownOrdinalTypes(compilation);

        // Static classes hosting the known-ordinal methods, used to avoid suppressing unrelated
        // user-defined methods that happen to share a name with a BCL method.
        private readonly HashSet<INamedTypeSymbol> _knownOrdinalContainerTypes = BuildKnownOrdinalContainerTypes(compilation);

        public INamedTypeSymbol? EqualityComparerStringType { get; } = GetIEqualityComparerString(compilation);
        public INamedTypeSymbol? ComparerStringType { get; } = GetIComparerString(compilation);
        public INamedTypeSymbol? EnumerableType { get; } = compilation.GetTypeByMetadataName("System.Linq.Enumerable");
        public INamedTypeSymbol? QueryableType { get; } = compilation.GetTypeByMetadataName("System.Linq.Queryable");
        public INamedTypeSymbol? ISetType { get; } = compilation.GetTypeByMetadataName("System.Collections.Generic.ISet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));
        public INamedTypeSymbol? IReadOnlySetType { get; } = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));
        public INamedTypeSymbol? IImmutableSetType { get; } = compilation.GetTypeByMetadataName("System.Collections.Immutable.IImmutableSet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));
        public INamedTypeSymbol? MeziantouFrameworkAssertType { get; } = compilation.GetTypeByMetadataName("Meziantou.Framework.Assertions.Assert");

        public void AnalyzeConstructor(OperationAnalysisContext ctx)
        {
            var operation = (IObjectCreationOperation)ctx.Operation;

            // Compiler-generated creations, such as interpolated string handlers, have no argument list
            // in the source code where a comparer could be added.
            if (operation.IsImplicit)
                return;

            if (HasEqualityComparerArgument(operation.Arguments))
                return;

            var method = operation.Constructor;
            if (method is null)
                return;

            if ((EqualityComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, options: default, [EqualityComparerStringType])) ||
                (ComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, options: default, [ComparerStringType])))
            {
                if (ctx.Options.GetConfigurationValue(operation, ReportOnlyNonOrdinalConfiguration) && IsKnownOrdinalType(operation.Type))
                    return;

                ctx.ReportDiagnostic(Rule, operation);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext ctx)
        {
            var operation = (IInvocationOperation)ctx.Operation;
            if (HasEqualityComparerArgument(operation.Arguments))
                return;

            if (_operationUtilities.IsInExpressionContext(operation))
                return;

            var method = operation.TargetMethod;

            // Most ISet implementation already configured the IEqualityComparer in this constructor,
            // so it should be ok to skip method calls on those types.
            // A concrete use-case is HashSet<string>.Contains which has an extension method IEnumerable.Contains(value, comparer)
            foreach (var type in (ReadOnlySpan<ITypeSymbol?>)[ISetType, IReadOnlySetType, IImmutableSetType])
            {

                if (type is null)
                    continue;

                if (method.ContainingType.IsOrImplements(type))
                    return;

                if (operation.Instance is not null && operation.Instance.GetActualType(ctx.CancellationToken)?.IsOrImplements(type) is true)
                    return;
            }

            if (operation.IsImplicit && IsQueryOperator(operation) && ctx.Options.GetConfigurationValue(operation, ExcludeQueryOperatorSyntaxesConfiguration))
                return;

            // Queryable comparer overloads are often not translatable by providers.
            if (QueryableType is not null && method.ContainingType.IsEqualTo(QueryableType))
                return;

            if (HasOverloadWithComparer(ctx, operation, out var namespaceToImport))
            {
                if (IsInvocationReportSuppressedByOrdinalOption(ctx, operation, method))
                    return;

                var properties = namespaceToImport is null
                    ? ImmutableDictionary<string, string?>.Empty
                    : ImmutableDictionary<string, string?>.Empty.Add(OverloadFinder.NamespaceToImportPropertyName, namespaceToImport);

                ctx.ReportDiagnostic(Rule, properties, operation, DefaultDiagnosticInvocationReportOptions);
                return;
            }

            if (EnumerableType is not null)
            {
                if (!method.ContainingType.IsEqualTo(EnumerableType))
                    return;

                if (method.Arity == 0)
                    return;

                if (method.Arity == 1)
                {
                    if (!EnumerableMethods.Contains(method.Name, StringComparer.Ordinal))
                        return;

                    if (!method.TypeArguments[0].IsString())
                        return;
                }
                else
                {
                    if (!ArityIndex.TryGetValue(method.Name, out var arityIndex))
                        return;

                    if (arityIndex >= method.Arity)
                        return;

                    if (!method.TypeArguments[arityIndex].IsString())
                        return;
                }

                if (!HasEqualityComparerArgument(operation.Arguments))
                {
                    if (IsInvocationReportSuppressedByOrdinalOption(ctx, operation, method))
                        return;

                    ctx.ReportDiagnostic(Rule, operation, DefaultDiagnosticInvocationReportOptions);
                }
            }
        }

        /// <summary>
        /// Indicates whether the invoked method has an overload with an <c>IEqualityComparer&lt;string&gt;</c> or an <c>IComparer&lt;string&gt;</c>
        /// parameter. An overload that does not require a new using directive is preferred. <paramref name="namespaceToImport"/> is set when the
        /// overload is an extension method declared in a namespace that is not imported.
        /// </summary>
        private bool HasOverloadWithComparer(OperationAnalysisContext context, IInvocationOperation operation, out string? namespaceToImport)
        {
            namespaceToImport = null;
            var includeExtensionMethodsFromNotImportedNamespaces = context.Options.GetConfigurationValue(operation, IncludeExtensionMethodsFromNotImportedNamespacesConfiguration);
            var options = new OverloadOptions { IncludeExtensionMethodsFromNotImportedNamespaces = includeExtensionMethodsFromNotImportedNamespaces };

            var found = false;
            foreach (var comparerType in (ReadOnlySpan<INamedTypeSymbol?>)[EqualityComparerStringType, ComparerStringType])
            {
                if (comparerType is null)
                    continue;

                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, options, [comparerType]);
                if (overload is null)
                    continue;

                var overloadNamespaceToImport = includeExtensionMethodsFromNotImportedNamespaces ? _overloadFinder.GetNamespaceToImport(overload, operation.Syntax) : null;
                if (overloadNamespaceToImport is null)
                {
                    namespaceToImport = null;
                    return true;
                }

                if (!found)
                {
                    found = true;
                    namespaceToImport = overloadNamespaceToImport;
                }
            }

            return found;
        }

#if ROSLYN_4_14_OR_GREATER
        public void AnalyzeCollectionExpression(OperationAnalysisContext ctx)
        {
            var operation = (ICollectionExpressionOperation)ctx.Operation;
            if (!ShouldReportCollectionExpression(ctx, operation))
                return;

#if ROSLYN_5_6_OR_GREATER
            // [with(StringComparer.Ordinal)] already provides a comparer — no diagnostic needed.
#pragma warning disable RSEXPERIMENTAL006
            if (HasEqualityComparerConstructArgument(operation.ConstructArguments))
                return;
#pragma warning restore RSEXPERIMENTAL006
#endif

            // ConstructMethod is the constructor (for types without [CollectionBuilder]) or the
            // factory method (for types with [CollectionBuilder], e.g. FrozenSet, ImmutableHashSet).
            // Either way, checking for an overload with an additional comparer parameter is the
            // correct way to decide whether the user should be prompted to specify a comparer.
            var constructMethod = operation.ConstructMethod;
            if (constructMethod is null)
                return;

            if ((EqualityComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(constructMethod, options: default, [EqualityComparerStringType])) ||
                (ComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(constructMethod, options: default, [ComparerStringType])))
            {
                if (ctx.Options.GetConfigurationValue(operation, ReportOnlyNonOrdinalConfiguration) && IsKnownOrdinalType(operation.Type))
                    return;

                ctx.ReportDiagnostic(Rule, operation);
            }

            static bool ShouldReportCollectionExpression(OperationAnalysisContext context, IOperation operation)
            {
                var defaultValue = operation.GetCSharpLanguageVersion().IsCSharp15OrGreater();
                return context.Options.GetConfigurationValue(operation, ReportCollectionExpressionsConfiguration, defaultValue);
            }
        }
#endif

        private static bool IsQueryOperator(IOperation operation)
        {
            var syntax = operation.Syntax;
            return syntax.IsKind(SyntaxKind.SelectClause)
                || syntax.IsKind(SyntaxKind.GroupClause)
                || syntax.IsKind(SyntaxKind.OrderByClause)
                || syntax.IsKind(SyntaxKind.AscendingOrdering)
                || syntax.IsKind(SyntaxKind.DescendingOrdering)
                || syntax.IsKind(SyntaxKind.JoinClause)
                || syntax.IsKind(SyntaxKind.JoinIntoClause);
        }

        private bool HasEqualityComparerArgument(ImmutableArray<IArgumentOperation> arguments)
        {
            foreach (var argument in arguments)
            {
                var argumentType = argument.Value.Type;
                if (argumentType is null)
                    continue;

                if (argumentType.GetAllInterfacesIncludingSelf().Any(i => EqualityComparerStringType.IsEqualTo(i) || ComparerStringType.IsEqualTo(i)))
                    return true;
            }

            return false;
        }

#if ROSLYN_5_6_OR_GREATER
#pragma warning disable RSEXPERIMENTAL006
        private bool HasEqualityComparerConstructArgument(ImmutableArray<IOperation> constructArguments)
        {
            foreach (var arg in constructArguments)
            {
                var argumentType = arg is IArgumentOperation argOp ? argOp.Value.Type : arg.Type;
                if (argumentType is null)
                    continue;

                if (argumentType.GetAllInterfacesIncludingSelf().Any(i => EqualityComparerStringType.IsEqualTo(i) || ComparerStringType.IsEqualTo(i)))
                    return true;
            }

            return false;
        }
#pragma warning restore RSEXPERIMENTAL006
#endif

        private bool IsInvocationReportSuppressedByOrdinalOption(OperationAnalysisContext ctx, IInvocationOperation operation, IMethodSymbol method)
        {
            if (!ctx.Options.GetConfigurationValue(operation, ReportOnlyNonOrdinalConfiguration))
                return false;

            if (method.ContainingType.IsEqualTo(MeziantouFrameworkAssertType))
                return true;

            return KnownOrdinalMethodNames.Contains(method.Name)
                && _knownOrdinalContainerTypes.Contains(method.ContainingType.OriginalDefinition);
        }

        private bool IsKnownOrdinalType(ITypeSymbol? type)
        {
            return type is INamedTypeSymbol namedType
                && _knownOrdinalTypes.Contains(namedType.OriginalDefinition);
        }

        private static HashSet<INamedTypeSymbol> BuildKnownOrdinalTypes(Compilation compilation)
        {
            var result = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.OrderedDictionary`2"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary`2"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableHashSet`1"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary`2"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenSet`1"));
            return result;
        }

        private static HashSet<INamedTypeSymbol> BuildKnownOrdinalContainerTypes(Compilation compilation)
        {
            var result = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Linq.Enumerable"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Linq.Queryable"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableDictionary"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableHashSet"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenDictionary"));
            result.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Frozen.FrozenSet"));
            return result;
        }

        private static INamedTypeSymbol? GetIEqualityComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
            if (equalityComparerInterfaceType is null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType is null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }

        private static INamedTypeSymbol? GetIComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetTypeByMetadataName("System.Collections.Generic.IComparer`1");
            if (equalityComparerInterfaceType is null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType is null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }
    }
}
