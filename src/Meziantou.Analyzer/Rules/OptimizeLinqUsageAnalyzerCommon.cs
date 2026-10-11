namespace Meziantou.Analyzer.Rules;

internal static class OptimizeLinqUsageAnalyzerCommon
{
    internal const string DataKey = "Data";
    internal const string MethodNameKey = "MethodName";
    internal const string ExpectedMethodNameKey = "ExpectedMethodName";
    internal const string FirstOperationStartKey = "FirstOperationStart";
    internal const string FirstOperationLengthKey = "FirstOperationLength";
    internal const string LastOperationStartKey = "LastOperationStart";
    internal const string LastOperationLengthKey = "LastOperationLength";
    internal const string CountOperationStartKey = "CountOperationStart";
    internal const string CountOperationLengthKey = "CountOperationLength";
    internal const string OperandOperationStartKey = "OperandOperationStart";
    internal const string OperandOperationLengthKey = "OperandOperationLength";
    internal const string SkipMinusOneKey = "SkipMinusOne";
    internal const string TakePlusOneKey = "TakePlusOne";
}
