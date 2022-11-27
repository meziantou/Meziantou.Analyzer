namespace Meziantou.Analyzer.Rules;
internal enum DoNotUseBlockingCallInAsyncContextData
{
    Unknown,
    Thread_Sleep,
    Task_Wait,
    Task_Wait_Delay,
    TaskAwaiter_GetResult,
    CreateAsyncScope,
    Overload,
    Task_Result,
    Using,
    UsingDeclarator,
}
