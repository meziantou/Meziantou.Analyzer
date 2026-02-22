using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

internal static class OptimizeStringBuilderUsageAnalyzerCommon
{
    public static string? GetConstStringValue(IOperation operation)
    {
        var sb = ObjectPool.SharedStringBuilderPool.Get();
        if (TryGetConstStringValue(operation, sb))
        {
            var result = sb.ToString();
            ObjectPool.SharedStringBuilderPool.Return(sb);
            return result;
        }

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

    public static bool HasFormatPlaceholders(string formatString)
    {
        var i = 0;
        while (i < formatString.Length)
        {
            var braceIndex = formatString.IndexOf('{', i);
            if (braceIndex == -1)
                return false;

            i = braceIndex;

            // Escaped opening brace
            if (i + 1 < formatString.Length && formatString[i + 1] == '{')
            {
                i += 2;
                continue;
            }

            // Check for {digit...}
            var j = i + 1;
            var hasDigit = false;
            while (j < formatString.Length && formatString[j] is >= '0' and <= '9')
            {
                hasDigit = true;
                j++;
            }

            if (hasDigit)
            {
                while (j < formatString.Length)
                {
                    if (formatString[j] == '}')
                        return true;

                    if (formatString[j] == '{')
                        break;

                    j++;
                }
            }

            i++;
        }

        return false;
    }
}
