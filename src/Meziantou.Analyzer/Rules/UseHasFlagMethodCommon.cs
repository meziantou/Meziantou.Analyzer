namespace Meziantou.Analyzer.Rules;

internal static class UseHasFlagMethodCommon
{
    /// <summary>
    /// Determines if both operations reference the same value, so that evaluating them twice is equivalent to evaluating them once.
    /// Only side-effect free references are supported (parameters, locals, fields), so that <c>(value &amp; flag) == flag</c> can safely be replaced by <c>value.HasFlag(flag)</c>.
    /// </summary>
    public static bool AreEquivalentOperands(IOperation? left, IOperation? right)
    {
        if (left is null || right is null)
            return false;

        left = left.UnwrapImplicitConversions();
        right = right.UnwrapImplicitConversions();

        return (left, right) switch
        {
            (IParameterReferenceOperation a, IParameterReferenceOperation b) => a.Parameter.IsEqualTo(b.Parameter),
            (ILocalReferenceOperation a, ILocalReferenceOperation b) => a.Local.IsEqualTo(b.Local),
            (IFieldReferenceOperation a, IFieldReferenceOperation b) => a.Field.IsEqualTo(b.Field) && !a.Field.IsVolatile && (a.Field.IsStatic || AreEquivalentOperands(a.Instance, b.Instance)),
            (IInstanceReferenceOperation a, IInstanceReferenceOperation b) => a.ReferenceKind == b.ReferenceKind && a.Type.IsEqualTo(b.Type),
            _ => false,
        };
    }
}
