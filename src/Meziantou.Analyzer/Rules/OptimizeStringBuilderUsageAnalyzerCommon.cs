namespace Meziantou.Analyzer.Rules;

internal static class OptimizeStringBuilderUsageAnalyzerCommon
{
    internal const string DataKey = "Data";
    internal const string ConstantValueKey = "ConstantValue";

    public static string? GetConstStringValue(IOperation operation)
    {
        var sb = ObjectPool.SharedStringBuilderPool.Get();
        try
        {
            return TryGetConstStringValue(operation, sb) ? sb.ToString() : null;
        }
        finally
        {
            ObjectPool.SharedStringBuilderPool.Return(sb);
        }
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

    /// <summary>
    /// Gets the text produced by a composite format string that contains no format item, such as <c>"{{text}}"</c> which produces <c>"{text}"</c>.
    /// Returns <see langword="false"/> when the format string contains a format item, or when it is invalid, as the formatting methods throw a <see cref="FormatException"/> in this case.
    /// </summary>
    public static bool TryGetCompositeFormatLiteralText(string formatString, [NotNullWhen(true)] out string? text)
    {
        if (formatString.IndexOfAny(['{', '}']) < 0)
        {
            text = formatString;
            return true;
        }

        var sb = ObjectPool.SharedStringBuilderPool.Get();
        try
        {
            for (var i = 0; i < formatString.Length; i++)
            {
                var c = formatString[i];
                if (c is '{' or '}')
                {
                    // A brace that is not doubled starts a format item, or is invalid
                    if (i + 1 >= formatString.Length || formatString[i + 1] != c)
                    {
                        text = null;
                        return false;
                    }

                    i++;
                }

                sb.Append(c);
            }

            text = sb.ToString();
            return true;
        }
        finally
        {
            ObjectPool.SharedStringBuilderPool.Return(sb);
        }
    }
}
