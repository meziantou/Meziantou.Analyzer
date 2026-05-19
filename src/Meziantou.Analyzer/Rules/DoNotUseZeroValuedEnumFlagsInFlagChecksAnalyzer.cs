using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseZeroValuedEnumFlagsInFlagChecksAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseZeroValuedEnumFlagsInFlagChecks,
        title: "Do not use zero-valued enum flags in flag checks",
        messageFormat: "This flag check is always '{0}' because the checked flag value is 0",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseZeroValuedEnumFlagsInFlagChecks));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeBinary(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            return;

        if (!TryGetZeroValuedFlagPattern(operation, out _))
            return;

        var isAlwaysTrue = operation.OperatorKind is BinaryOperatorKind.Equals;
        context.ReportDiagnostic(Rule, operation, isAlwaysTrue ? "true" : "false");
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var operation = (IIsPatternOperation)context.Operation;
        if (!TryGetZeroValuedFlagPattern(operation, out var isAlwaysTrue))
            return;

        context.ReportDiagnostic(Rule, operation, isAlwaysTrue ? "true" : "false");
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (!IsZeroValuedHasFlagInvocation(operation))
            return;

        context.ReportDiagnostic(Rule, operation, "true");
    }

    private static bool TryGetZeroValuedFlagPattern(IOperation operation, out bool isAlwaysTrue)
    {
        if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binaryOperation)
        {
            if (TryGetZeroValuedFlagPattern(binaryOperation))
            {
                isAlwaysTrue = binaryOperation.OperatorKind is BinaryOperatorKind.Equals;
                return true;
            }
        }
        else if (operation is IIsPatternOperation
                 {
                     Value: IBinaryOperation { OperatorKind: BinaryOperatorKind.And } andOperation,
                     Pattern: var patternOperation,
                 } &&
                 TryGetComparedOperand(patternOperation, out var comparedOperand, out var negateResult) &&
                 IsZeroValuedFlagCheck(andOperation, comparedOperand))
        {
            isAlwaysTrue = !negateResult;
            return true;
        }

        isAlwaysTrue = false;
        return false;
    }

    private static bool TryGetZeroValuedFlagPattern(IBinaryOperation operation)
    {
        var leftOperand = operation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = operation.RightOperand.UnwrapImplicitConversionOperations();
        if (leftOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } leftBitwiseAnd &&
            IsZeroValuedFlagCheck(leftBitwiseAnd, rightOperand))
        {
            return true;
        }

        if (rightOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } rightBitwiseAnd &&
            IsZeroValuedFlagCheck(rightBitwiseAnd, leftOperand))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetComparedOperand(IPatternOperation patternOperation, out IOperation comparedOperand, out bool negateResult)
    {
        if (patternOperation is IConstantPatternOperation { Value: not null } constantPattern)
        {
            comparedOperand = constantPattern.Value;
            negateResult = false;
            return true;
        }

        if (patternOperation is INegatedPatternOperation { Pattern: IConstantPatternOperation { Value: not null } negatedConstantPattern })
        {
            comparedOperand = negatedConstantPattern.Value;
            negateResult = true;
            return true;
        }

        comparedOperand = null!;
        negateResult = false;
        return false;
    }

    private static bool IsZeroValuedFlagCheck(IBinaryOperation bitwiseAndOperation, IOperation comparedOperand)
    {
        var leftOperand = bitwiseAndOperation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = bitwiseAndOperation.RightOperand.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();
        if (TryGetZeroValuedEnumFlagType(rightOperand, comparedOperand, out var enumType) &&
            IsValidPattern(leftOperand, enumType))
        {
            return true;
        }

        if (TryGetZeroValuedEnumFlagType(leftOperand, comparedOperand, out enumType) &&
            IsValidPattern(rightOperand, enumType))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetZeroValuedEnumFlagType(IOperation potentialFlag, IOperation comparedOperand, out ITypeSymbol enumType)
    {
        potentialFlag = potentialFlag.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();
        if (potentialFlag is not IFieldReferenceOperation firstFieldReference ||
            !firstFieldReference.Field.HasConstantValue ||
            !firstFieldReference.Field.ContainingType.IsEnumeration() ||
            !NumericHelpers.IsZero(firstFieldReference.Field.ConstantValue))
        {
            enumType = null!;
            return false;
        }

        if (!IsComparedOperandZero(comparedOperand, firstFieldReference.Field.ContainingType))
        {
            enumType = null!;
            return false;
        }

        enumType = firstFieldReference.Field.ContainingType;
        return true;
    }

    private static bool IsComparedOperandZero(IOperation comparedOperand, ITypeSymbol enumType)
    {
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();
        if (comparedOperand is IFieldReferenceOperation comparedFieldReference &&
            comparedFieldReference.Field.HasConstantValue &&
            comparedFieldReference.Field.ContainingType.IsEqualTo(enumType) &&
            NumericHelpers.IsZero(comparedFieldReference.Field.ConstantValue))
        {
            return true;
        }

        if (comparedOperand.IsConstantZero())
            return true;

        if (comparedOperand.Type?.IsEqualTo(enumType) is true &&
            comparedOperand.ConstantValue.HasValue &&
            NumericHelpers.IsZero(comparedOperand.ConstantValue.Value))
        {
            return true;
        }

        return false;
    }

    private static bool IsValidPattern(IOperation enumValueOperation, ITypeSymbol flagType)
    {
        if (enumValueOperation.Type is null)
            return false;

        if (!enumValueOperation.Type.IsEnumeration())
            return false;

        return enumValueOperation.Type.IsEqualTo(flagType);
    }

    private static bool IsZeroValuedHasFlagInvocation(IInvocationOperation operation)
    {
        if (operation.TargetMethod.Name is not nameof(Enum.HasFlag) and not "HasFlags")
            return false;

        IOperation? enumValueOperation;
        var flagArgumentIndex = 0;
        if (operation.Instance is not null)
        {
            enumValueOperation = operation.Instance.UnwrapImplicitConversionOperations();
        }
        else if (operation.TargetMethod.IsExtensionMethod && operation.Arguments.Length >= 2)
        {
            enumValueOperation = operation.Arguments[0].Value.UnwrapImplicitConversionOperations();
            flagArgumentIndex = 1;
        }
        else
        {
            return false;
        }

        if (enumValueOperation.Type is null || !enumValueOperation.Type.IsEnumeration())
            return false;

        if (operation.Arguments.Length <= flagArgumentIndex)
            return false;

        return IsComparedOperandZero(operation.Arguments[flagArgumentIndex].Value, enumValueOperation.Type);
    }
}
