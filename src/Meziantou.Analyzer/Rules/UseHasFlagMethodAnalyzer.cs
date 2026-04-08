using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseHasFlagMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseHasFlagMethod,
        title: "Use HasFlag instead of bitwise checks",
        messageFormat: "Use HasFlag instead of bitwise checks",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseHasFlagMethod));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeBinary, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeIsPattern, OperationKind.IsPattern);
    }

    private static void AnalyzeBinary(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals &&
            TryGetHasFlagPattern(operation, out _))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static void AnalyzeIsPattern(OperationAnalysisContext context)
    {
        var operation = (IIsPatternOperation)context.Operation;
        if (TryGetHasFlagPattern(operation, out _))
        {
            context.ReportDiagnostic(Rule, operation);
        }
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
            if (TryGetComparedOperand(patternOperation, out var comparedOperand))
            {
                pattern = GetFromBitwiseAnd(andOperation, comparedOperand);
                return pattern is not null;
            }
        }

        pattern = null;
        return false;
    }

    private static bool TryGetComparedOperand(IPatternOperation patternOperation, [NotNullWhen(true)] out IOperation? comparedOperand)
    {
        if (patternOperation is IConstantPatternOperation { Value: not null } constantPattern)
        {
            comparedOperand = constantPattern.Value;
            return true;
        }

        if (patternOperation is INegatedPatternOperation { Pattern: IConstantPatternOperation { Value: not null } negatedConstantPattern })
        {
            comparedOperand = negatedConstantPattern.Value;
            return true;
        }

        comparedOperand = null;
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
            comparedOperand is IFieldReferenceOperation secondFieldReference &&
            firstFieldReference.Field.HasConstantValue &&
            secondFieldReference.Field.HasConstantValue &&
            firstFieldReference.Field.IsEqualTo(secondFieldReference.Field) &&
            firstFieldReference.Field.ContainingType.IsEnumeration())
        {
            flagOperation = secondFieldReference;
            return true;
        }

        flagOperation = null;
        return false;
    }

    private static bool IsValidPattern(IOperation enumValueOperation, IOperation flagOperation)
    {
        if (enumValueOperation.Type is null || flagOperation.Type is null)
            return false;

        if (!enumValueOperation.Type.IsEnumeration())
            return false;

        if (!flagOperation.Type.IsEnumeration())
            return false;

        return enumValueOperation.Type.IsEqualTo(flagOperation.Type);
    }

    private sealed record HasFlagPattern(IOperation EnumValueOperation, IOperation FlagOperation);
}
