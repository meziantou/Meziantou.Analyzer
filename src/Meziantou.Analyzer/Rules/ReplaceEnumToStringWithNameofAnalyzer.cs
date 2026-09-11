namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReplaceEnumToStringWithNameofAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ReplaceEnumToStringWithNameof,
        title: "Replace constant Enum.ToString with nameof",
        messageFormat: "Replace constant Enum.ToString with nameof",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReplaceEnumToStringWithNameof));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.TargetMethod.Name != nameof(object.ToString))
            return;

        if (!operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetSpecialType(SpecialType.System_Enum)))
            return;

        if (!IsUniquelyNamedEnumMember(operation.Instance))
            return;

        if (operation.Arguments.Length > 0)
        {
            var format = operation.Arguments[0].Value;
            if (format is { ConstantValue: { HasValue: true, Value: var formatValue } })
            {
                if (!IsNameFormat(formatValue))
                    return;
            }
            else
            {
                return;
            }
        }

        context.ReportDiagnostic(Rule, operation);
    }

    private static void AnalyzeInterpolation(OperationAnalysisContext context)
    {
        var operation = (IInterpolationOperation)context.Operation;
        if (!IsUniquelyNamedEnumMember(operation.Expression))
            return;

        if (operation.FormatString is ILiteralOperation { ConstantValue: { HasValue: true, Value: var format } })
        {
            if (!IsNameFormat(format))
                return;
        }

        context.ReportDiagnostic(Rule, operation);
    }

    // Enum.ToString formats the value, so when several members share the same value,
    // it may return the name of another member than the one referenced in the source code.
    private static bool IsUniquelyNamedEnumMember(IOperation? operation)
    {
        if (operation is not IFieldReferenceOperation { Field: { HasConstantValue: true } field })
            return false;

        var enumType = field.ContainingType;
        if (enumType.EnumUnderlyingType is null)
            return false;

        foreach (var member in enumType.GetMembers())
        {
            if (member is IFieldSymbol { HasConstantValue: true } otherField && !otherField.IsEqualTo(field) && Equals(otherField.ConstantValue, field.ConstantValue))
                return false;
        }

        return true;
    }

    private static bool IsNameFormat(object? format)
    {
        if (format is null)
            return true;

        if (format is string str && str is "g" or "G" or "f" or "F" or "")
            return true;

        return false;
    }
}
