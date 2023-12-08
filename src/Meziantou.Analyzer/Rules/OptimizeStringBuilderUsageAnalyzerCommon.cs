using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal static class OptimizeStringBuilderUsageAnalyzerCommon
{
    public static string? GetConstStringValue(IOperation operation)
    {
        var sb = new StringBuilder();
        if (TryGetConstStringValue(operation, sb))
            return sb.ToString();

        return null;
    }

    public static bool TryGetConstStringValue(IOperation operation, StringBuilder sb)
    {
        if (operation is null)
            return false;

        if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is string str)
        {
            sb.Append(str);
            return true;
        }

        if (operation is IInterpolatedStringOperation interpolationStringOperation)
        {
            foreach (var part in interpolationStringOperation.Parts)
            {
                if (!TryGetConstStringValue(part, sb))
                    return false;
            }

            return true;
        }

        if (operation is IInterpolatedStringTextOperation text)
        {
            if (!TryGetConstStringValue(text.Text, sb))
                return false;

            return true;
        }

        if (operation is IInterpolatedStringContentOperation interpolated)
        {
            var op = interpolated.GetChildOperations().SingleOrDefaultIfMultiple();
            if (op is null)
                return false;

            return TryGetConstStringValue(op, sb);
        }

        if (operation is IMemberReferenceOperation memberReference)
        {
            if (string.Equals(memberReference.Member.Name, nameof(string.Empty), System.StringComparison.Ordinal) && memberReference.Member.ContainingType.IsString())
            {
                return true;
            }
        }

        return false;
    }
}
