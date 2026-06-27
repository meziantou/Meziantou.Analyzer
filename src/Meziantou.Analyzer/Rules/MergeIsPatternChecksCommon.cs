using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal static class MergeIsPatternChecksCommon
{
    public static bool TryGetMergeTarget(IOperation operation, out MergeTarget mergeTarget)
    {
        operation = UnwrapOperation(operation);
        switch (operation)
        {
            case ILocalReferenceOperation localReferenceOperation:
                mergeTarget = new(localReferenceOperation.Local, null, ImmutableArray<ExpressionSyntax>.Empty);
                return true;
            case IParameterReferenceOperation parameterReferenceOperation:
                mergeTarget = new(parameterReferenceOperation.Parameter, null, ImmutableArray<ExpressionSyntax>.Empty);
                return true;
            case IFieldReferenceOperation fieldReferenceOperation when TryGetOptionalMergeTarget(fieldReferenceOperation.Instance, out var fieldInstance):
                mergeTarget = new(fieldReferenceOperation.Field, fieldInstance, ImmutableArray<ExpressionSyntax>.Empty);
                return true;
            case IPropertyReferenceOperation propertyReferenceOperation
                when TryGetOptionalMergeTarget(propertyReferenceOperation.Instance, out var propertyInstance) &&
                     TryGetMergeTargetArguments(propertyReferenceOperation.Arguments, out var propertyArguments):
                mergeTarget = new(propertyReferenceOperation.Property, propertyInstance, propertyArguments);
                return true;
            case IEventReferenceOperation eventReferenceOperation when TryGetOptionalMergeTarget(eventReferenceOperation.Instance, out var eventInstance):
                mergeTarget = new(eventReferenceOperation.Event, eventInstance, ImmutableArray<ExpressionSyntax>.Empty);
                return true;
            case IInstanceReferenceOperation instanceReferenceOperation when instanceReferenceOperation.Type is not null:
                mergeTarget = new(instanceReferenceOperation.Type, null, ImmutableArray<ExpressionSyntax>.Empty);
                return true;
            default:
                mergeTarget = null!;
                return false;
        }
    }

    public static bool AreSameMergeTarget(MergeTarget left, MergeTarget right)
    {
        return SymbolEqualityComparer.Default.Equals(left.Symbol, right.Symbol) &&
               AreSameOptionalMergeTarget(left.Instance, right.Instance) &&
               AreSameMergeTargetArguments(left.Arguments, right.Arguments);
    }

    private static IOperation UnwrapOperation(IOperation operation)
    {
        operation = operation.UnwrapConversionOperations();
        while (operation is IParenthesizedOperation parenthesizedOperation)
        {
            operation = parenthesizedOperation.Operand.UnwrapConversionOperations();
        }

        return operation;
    }

    private static bool TryGetMergeTargetArguments(ImmutableArray<IArgumentOperation> arguments, out ImmutableArray<ExpressionSyntax> mergeTargetArguments)
    {
        if (arguments.IsDefaultOrEmpty)
        {
            mergeTargetArguments = ImmutableArray<ExpressionSyntax>.Empty;
            return true;
        }

        var builder = ImmutableArray.CreateBuilder<ExpressionSyntax>(arguments.Length);
        foreach (var argument in arguments)
        {
            if (argument.Value.Syntax is not ExpressionSyntax expressionSyntax)
            {
                mergeTargetArguments = default;
                return false;
            }

            builder.Add(expressionSyntax);
        }

        mergeTargetArguments = builder.MoveToImmutable();
        return true;
    }

    private static bool TryGetOptionalMergeTarget(IOperation? operation, out MergeTarget? mergeTarget)
    {
        if (operation is null)
        {
            mergeTarget = null;
            return true;
        }

        if (TryGetMergeTarget(operation, out var target))
        {
            mergeTarget = target;
            return true;
        }

        mergeTarget = null;
        return false;
    }

    private static bool AreSameOptionalMergeTarget(MergeTarget? left, MergeTarget? right)
    {
        if (left is null)
            return right is null;

        if (right is null)
            return false;

        return AreSameMergeTarget(left, right);
    }

    private static bool AreSameMergeTargetArguments(ImmutableArray<ExpressionSyntax> left, ImmutableArray<ExpressionSyntax> right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].IsEquivalentTo(right[i], topLevel: false))
                return false;
        }

        return true;
    }

    public sealed record class MergeTarget(ISymbol Symbol, MergeTarget? Instance, ImmutableArray<ExpressionSyntax> Arguments);
}
