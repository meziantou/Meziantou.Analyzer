namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BothSideOfTheConditionAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.BothSideOfTheConditionAreIdentical,
        title: "Both sides of the logical operation are identical",
        messageFormat: "Both sides of the logical operation are identical",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.BothSideOfTheConditionAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);


        context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeBinaryPatternOperation, OperationKind.BinaryPattern);
    }

    private void AnalyzeBinaryOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.And or BinaryOperatorKind.Or or BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
        {
            if (operation.Type.IsBoolean() && operation.LeftOperand.Syntax.IsEquivalentTo(operation.RightOperand.Syntax, topLevel: false) && !MayProduceDifferentValues(operation.LeftOperand))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }

    /// <summary>
    /// Evaluating the same code twice can produce different values when the code has side effects (e.g. <c>e.MoveNext() &amp;&amp; e.MoveNext()</c>)
    /// or creates a new instance (e.g. <c>new object() == new object()</c>).
    /// </summary>
    private static bool MayProduceDifferentValues(IOperation operation)
    {
        foreach (var child in operation.DescendantsAndSelf())
        {
            if (child is IInvocationOperation
                or IDynamicInvocationOperation
                or IIncrementOrDecrementOperation
                or IAssignmentOperation
                or IEventAssignmentOperation
                or IObjectCreationOperation
                or IDynamicObjectCreationOperation
                or ITypeParameterObjectCreationOperation
                or IAnonymousObjectCreationOperation
                or IAwaitOperation)
            {
                return true;
            }
        }

        return false;
    }

    private void AnalyzeBinaryPatternOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryPatternOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.And or BinaryOperatorKind.Or)
        {
            if (operation.LeftPattern.Syntax.IsEquivalentTo(operation.RightPattern.Syntax, topLevel: false))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }
}
