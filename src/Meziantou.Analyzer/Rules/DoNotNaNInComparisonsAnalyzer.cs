namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotNaNInComparisonsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotNaNInComparisons,
        title: "NaN should not be used in comparisons",
        messageFormat: "{0}.NaN should not be used in comparisons",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotNaNInComparisons));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeBinaryOperator, OperationKind.Binary);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        public ISymbol? DoubleNaN { get; } = compilation.GetBestTypeByMetadataName("System.Double")?.GetMembers("NaN").FirstOrDefault();
        public ISymbol? SingleNaN { get; } = compilation.GetBestTypeByMetadataName("System.Single")?.GetMembers("NaN").FirstOrDefault();
        public ISymbol? HalfNaN { get; } = compilation.GetBestTypeByMetadataName("System.Half")?.GetMembers("NaN").FirstOrDefault();
        public INamedTypeSymbol? FloatingPointIeee754 { get; } = compilation.GetBestTypeByMetadataName("System.Numerics.IFloatingPointIeee754`1");

        public void AnalyzeBinaryOperator(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals
                or BinaryOperatorKind.LessThan or BinaryOperatorKind.LessThanOrEqual
                or BinaryOperatorKind.GreaterThan or BinaryOperatorKind.GreaterThanOrEqual)
            {
                AnalyzeOperand(context, operation.LeftOperand);
                AnalyzeOperand(context, operation.RightOperand);
            }
        }

        private void AnalyzeOperand(OperationAnalysisContext context, IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            if (operation is IMemberReferenceOperation memberReference)
            {
                if (memberReference.Member.IsEqualTo(DoubleNaN))
                {
                    context.ReportDiagnostic(Rule, operation, "System.Double");
                }
                else if (memberReference.Member.IsEqualTo(SingleNaN))
                {
                    context.ReportDiagnostic(Rule, operation, "System.Single");
                }
                else if (memberReference.Member.IsEqualTo(HalfNaN))
                {
                    context.ReportDiagnostic(Rule, operation, "System.Half");
                }
                else if (GetIeee754NaNType(memberReference.Member) is { } type)
                {
                    // Generic math: T.NaN where T is constrained to IFloatingPointIeee754<T>
                    context.ReportDiagnostic(Rule, operation, type.ToDisplayString());
                }
            }
        }

        private ITypeSymbol? GetIeee754NaNType(ISymbol member)
        {
            if (member is { Name: "NaN", ContainingType: { TypeArguments: [{ } selfType] } containingType } &&
                containingType.OriginalDefinition.IsEqualTo(FloatingPointIeee754))
                return selfType;

            return null;
        }
    }
}
