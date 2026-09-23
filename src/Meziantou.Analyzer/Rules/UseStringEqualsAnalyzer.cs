namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringEqualsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStringEqualsInsteadOfEqualityOperator,
        title: "Use String.Equals instead of equality operator",
        messageFormat: "Use string.Equals instead of {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringEqualsInsteadOfEqualityOperator));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var operationUtilities = new OperationUtilities(context.Compilation);
            context.RegisterOperationAction(context => AnalyzeInvocation(context, operationUtilities), OperationKind.BinaryOperator);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, OperationUtilities operationUtilities)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
        {
            if (operation.LeftOperand.Type.IsString() && operation.RightOperand.Type.IsString())
            {
                if (IsNull(operation.LeftOperand) || IsNull(operation.RightOperand))
                    return;

                if (IsStringEmpty(operation.LeftOperand) || IsStringEmpty(operation.RightOperand))
                    return;

                if (IsInConstantContext(operation))
                    return;

                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (operationUtilities.IsInExpressionContext(operation))
                    return;

                context.ReportDiagnostic(Rule, operation, $"{operation.OperatorKind} operator");
            }
        }
    }

    // string.Equals is not a constant expression, so it cannot replace the operator where a constant expression is required
    private static bool IsInConstantContext(IOperation operation)
    {
        if (!operation.ConstantValue.HasValue)
            return false;

        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            switch (parent)
            {
                case IFieldInitializerOperation fieldInitializer:
                    return fieldInitializer.InitializedFields.Any(field => field.IsConst);

                case IVariableInitializerOperation { Parent: IVariableDeclaratorOperation declarator }:
                    return declarator.Symbol.IsConst;

                case IParameterInitializerOperation:
                case ISingleValueCaseClauseOperation:
                case IPatternOperation:
                case IAttributeOperation:
                    return true;
            }
        }

        return false;
    }

    private static bool IsNull(IOperation operation)
    {
        return operation.ConstantValue.HasValue && operation.ConstantValue.Value is null;
    }

    private static bool IsStringEmpty(IOperation operation)
    {
        if (operation is { ConstantValue: { HasValue: true, Value: null or string { Length: 0 } } })
            return true;

        if (operation is IMemberReferenceOperation memberReferenceOperation && memberReferenceOperation.Member.ContainingType.IsString() && memberReferenceOperation.Member.Name == nameof(string.Empty))
            return true;

        return false;
    }
}
