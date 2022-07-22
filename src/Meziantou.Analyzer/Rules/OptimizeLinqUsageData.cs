namespace Meziantou.Analyzer.Rules;

internal enum OptimizeLinqUsageData
{
    None,
    UseLengthProperty,
    UseLongLengthProperty,
    UseCountProperty,
    UseFindMethod,
    UseFindMethodWithConversion,
    UseIndexer,
    UseIndexerFirst,
    UseIndexerLast,
    DuplicatedOrderBy,
    CombineWhereWithNextMethod,
    UseFalse,
    UseTrue,
    UseAny,
    UseNotAny,
    UseTakeAndCount,
    UseSkipAndNotAny,
    UseSkipAndAny,
    UseCastInsteadOfSelect,
}
