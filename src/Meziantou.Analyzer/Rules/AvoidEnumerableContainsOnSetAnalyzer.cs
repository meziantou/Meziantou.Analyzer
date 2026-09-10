namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidEnumerableContainsOnSetAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.AvoidEnumerableContainsOnSet,
        title: "Avoid using 'Enumerable.Contains' on a set",
        messageFormat: "Avoid using 'Enumerable.Contains' on a set as it does a linear search instead of using the set lookup ({0})",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidEnumerableContainsOnSet));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (!analyzerContext.IsValid)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private INamedTypeSymbol? EnumerableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Linq.Enumerable");
        private INamedTypeSymbol? ISetSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.ISet`1");
        private INamedTypeSymbol? IReadOnlySetSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1");
        private INamedTypeSymbol? IImmutableSetSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableSet`1");

        public bool IsValid => EnumerableSymbol is not null && (ISetSymbol is not null || IReadOnlySetSymbol is not null || IImmutableSetSymbol is not null);

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;
            if (method is not { Name: "Contains", IsExtensionMethod: true, TypeArguments: [var valueType] })
                return;

            if (!method.ContainingType.IsEqualTo(EnumerableSymbol))
                return;

            // Enumerable.Contains(source, value) or Enumerable.Contains(source, value, comparer)
            if (operation.Arguments.Length is not (2 or 3))
                return;

            var sourceType = operation.Arguments[0].Value.GetActualType(useDataFlowAnalysis: true, context.CancellationToken);
            if (sourceType is null)
                return;

            if (GetSetElementType(sourceType) is not { } elementType)
                return;

            // Enumerable.Contains(source, value) uses ICollection<T>.Contains when the source implements ICollection<T>,
            // so the set lookup is used. This is not the case when a comparer is provided, or when the value type is not
            // the element type of the set, such as when the set is used as a covariant IEnumerable<T>.
            if (operation.Arguments.Length is 3)
            {
                context.ReportDiagnostic(Rule, operation, "the comparer of the set is not used");
            }
            else if (!elementType.IsEqualTo(valueType))
            {
                context.ReportDiagnostic(Rule, operation, $"the set is used as '{valueType.ToDisplayString()}' instead of '{elementType.ToDisplayString()}'");
            }
        }

        private ITypeSymbol? GetSetElementType(ITypeSymbol type)
        {
            foreach (var setSymbol in (ReadOnlySpan<INamedTypeSymbol?>)[ISetSymbol, IReadOnlySetSymbol, IImmutableSetSymbol])
            {
                if (setSymbol is null)
                    continue;

                if (type.OriginalDefinition.IsEqualTo(setSymbol))
                    return ((INamedTypeSymbol)type).TypeArguments[0];

                foreach (var iface in type.AllInterfaces)
                {
                    if (iface.OriginalDefinition.IsEqualTo(setSymbol))
                        return iface.TypeArguments[0];
                }
            }

            return null;
        }
    }
}
