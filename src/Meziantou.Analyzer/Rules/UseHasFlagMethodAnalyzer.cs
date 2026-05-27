using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseHasFlagMethodAnalyzer : DiagnosticAnalyzer
{
    private const string EnumHasFlagMethodDocumentationId = "M:System.Enum.HasFlag(System.Enum)";

    private static readonly DiagnosticDescriptor UseHasFlagRule = new(
        RuleIdentifiers.UseHasFlagMethod,
        title: "Use HasFlag instead of bitwise checks",
        messageFormat: "Use HasFlag instead of bitwise checks",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseHasFlagMethod));

    private static readonly DiagnosticDescriptor DoNotUseZeroValuedEnumFlagsInFlagChecksRule = new(
        RuleIdentifiers.DoNotUseZeroValuedEnumFlagsInFlagChecks,
        title: "Do not use zero-valued enum flags in flag checks",
        messageFormat: "This flag check is always '{0}' because the checked flag value is 0",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseZeroValuedEnumFlagsInFlagChecks));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UseHasFlagRule, DoNotUseZeroValuedEnumFlagsInFlagChecksRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var hasFlagMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId(EnumHasFlagMethodDocumentationId, compilationContext.Compilation) as IMethodSymbol;

            compilationContext.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
            compilationContext.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);

            if (hasFlagMethod is not null)
            {
                compilationContext.RegisterOperationAction(context => AnalyzeInvocation(context, hasFlagMethod), OperationKind.Invocation);
            }
        });
    }

    private static void AnalyzeBinary(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            return;

        if (TryGetZeroValuedFlagPattern(operation, out var isAlwaysTrue))
        {
            context.ReportDiagnostic(DoNotUseZeroValuedEnumFlagsInFlagChecksRule, operation, isAlwaysTrue ? "true" : "false");
            return;
        }

        if (TryGetHasFlagPattern(operation, out _))
        {
            context.ReportDiagnostic(UseHasFlagRule, operation);
        }
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var operation = (IIsPatternOperation)context.Operation;
        if (TryGetZeroValuedFlagPattern(operation, out var isAlwaysTrue))
        {
            context.ReportDiagnostic(DoNotUseZeroValuedEnumFlagsInFlagChecksRule, operation, isAlwaysTrue ? "true" : "false");
            return;
        }

        if (TryGetHasFlagPattern(operation, out _))
        {
            context.ReportDiagnostic(UseHasFlagRule, operation);
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol hasFlagMethod)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (!IsZeroValuedHasFlagInvocation(operation, hasFlagMethod))
            return;

        context.ReportDiagnostic(DoNotUseZeroValuedEnumFlagsInFlagChecksRule, operation, "true");
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

    private static bool IsZeroValuedHasFlagInvocation(IInvocationOperation operation, IMethodSymbol hasFlagMethod)
    {
        if (!operation.TargetMethod.OriginalDefinition.IsEqualTo(hasFlagMethod))
            return false;

        if (operation.Arguments.Length is not 1 || operation.Instance is null)
            return false;

        var enumValueOperation = operation.Instance.UnwrapImplicitConversionOperations();
        if (enumValueOperation.Type is null || !enumValueOperation.Type.IsEnumeration())
            return false;

        return IsComparedOperandZero(operation.Arguments[0].Value, enumValueOperation.Type);
    }

    private static bool TryGetComparedOperand(IPatternOperation patternOperation, [NotNullWhen(true)] out IOperation? comparedOperand, out bool negateResult)
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

        comparedOperand = null;
        negateResult = false;
        return false;
    }

    private static bool TryGetHasFlagPattern(IOperation operation, [NotNullWhen(true)] out HasFlagPattern? pattern)
    {
        if (operation is IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } binaryOperation)
        {
            pattern = GetFromBinaryComparison(binaryOperation);
            return pattern is not null;
        }

        if (operation is IIsPatternOperation
            {
                Value: IBinaryOperation { OperatorKind: BinaryOperatorKind.And } andOperation,
                Pattern: IPatternOperation patternOperation,
            })
        {
            if (TryGetComparedOperand(patternOperation, out var comparedOperand, out _))
            {
                pattern = GetFromBitwiseAnd(andOperation, comparedOperand);
                return pattern is not null;
            }
        }

        pattern = null;
        return false;
    }

    private static HasFlagPattern? GetFromBinaryComparison(IBinaryOperation operation)
    {
        var leftOperand = operation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = operation.RightOperand.UnwrapImplicitConversionOperations();

        if (leftOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } leftBitwiseAnd)
        {
            var pattern = GetFromBitwiseAnd(leftBitwiseAnd, rightOperand);
            if (pattern is not null)
                return pattern;
        }

        if (rightOperand is IBinaryOperation { OperatorKind: BinaryOperatorKind.And } rightBitwiseAnd)
        {
            var pattern = GetFromBitwiseAnd(rightBitwiseAnd, leftOperand);
            if (pattern is not null)
                return pattern;
        }

        return null;
    }

    private static HasFlagPattern? GetFromBitwiseAnd(IBinaryOperation bitwiseAndOperation, IOperation comparedOperand)
    {
        var leftOperand = bitwiseAndOperation.LeftOperand.UnwrapImplicitConversionOperations();
        var rightOperand = bitwiseAndOperation.RightOperand.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();

        if (TryGetEnumFlagReference(rightOperand, comparedOperand, out var flagOperation) &&
            IsValidPattern(leftOperand, flagOperation))
        {
            return new(leftOperand, flagOperation);
        }

        if (TryGetEnumFlagReference(leftOperand, comparedOperand, out flagOperation) &&
            IsValidPattern(rightOperand, flagOperation))
        {
            return new(rightOperand, flagOperation);
        }

        return null;
    }

    private static bool TryGetEnumFlagReference(IOperation potentialFlag, IOperation comparedOperand, [NotNullWhen(true)] out IFieldReferenceOperation? flagOperation)
    {
        potentialFlag = potentialFlag.UnwrapImplicitConversionOperations();
        comparedOperand = comparedOperand.UnwrapImplicitConversionOperations();

        if (potentialFlag is IFieldReferenceOperation firstFieldReference &&
            firstFieldReference.Field.HasConstantValue &&
            firstFieldReference.Field.ContainingType.IsEnumeration())
        {
            if (comparedOperand is IFieldReferenceOperation secondFieldReference &&
                secondFieldReference.Field.HasConstantValue &&
                firstFieldReference.Field.IsEqualTo(secondFieldReference.Field) &&
                !NumericHelpers.IsZero(firstFieldReference.Field.ConstantValue))
            {
                flagOperation = secondFieldReference;
                return true;
            }

            if (comparedOperand.IsConstantZero() && NumericHelpers.IsSingleBitSet(firstFieldReference.Field.ConstantValue))
            {
                flagOperation = firstFieldReference;
                return true;
            }
        }

        flagOperation = null;
        return false;
    }

    private static bool IsValidPattern(IOperation enumValueOperation, IOperation flagOperation)
    {
        if (flagOperation.Type is null)
            return false;

        return IsValidPattern(enumValueOperation, flagOperation.Type);
    }

    private static bool IsValidPattern(IOperation enumValueOperation, ITypeSymbol flagType)
    {
        if (enumValueOperation.Type is null)
            return false;

        if (!enumValueOperation.Type.IsEnumeration())
            return false;

        if (!flagType.IsEnumeration())
            return false;

        return enumValueOperation.Type.IsEqualTo(flagType);
    }

    private sealed record HasFlagPattern(IOperation EnumValueOperation, IOperation FlagOperation);
}
