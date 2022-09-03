namespace Meziantou.Analyzer.Rules;

internal enum OptimizeStringBuilderUsageData
{
    None,
    RemoveArgument,
    RemoveMethod,
    ReplaceWithChar,
    SplitStringInterpolation,
    SplitAddOperator,
    RemoveToString,
    ReplaceToStringWithAppendFormat,
    ReplaceStringFormatWithAppendFormat,
    ReplaceSubstring,
    ReplaceStringJoinWithAppendJoin,
}
