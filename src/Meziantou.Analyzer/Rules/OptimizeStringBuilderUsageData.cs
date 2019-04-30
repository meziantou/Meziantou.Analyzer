namespace Meziantou.Analyzer.Rules
{
    internal enum OptimizeStringBuilderUsageData
    {
        None,
        RemoveArgument,
        RemoveMethod,
        ReplaceWithChar,
        SplitStringInterpolation,
        SplitAddOperator,
        RemoveToString,
        ReplaceWithAppendFormat,
        ReplaceSubstring,
    }
}
