namespace Meziantou.Analyzer.Rules;

internal static class ReturnTaskInsteadOfAwaitingItCommon
{
    private const int ContinueOnCapturedContext = 1; // System.Threading.Tasks.ConfigureAwaitOptions.ContinueOnCapturedContext

    /// <summary>
    /// Indicates whether a <c>ConfigureAwait</c> call can be dropped together with the <c>await</c> it configures.
    /// </summary>
    /// <remarks>
    /// The configuration must be a constant, as a non-constant expression must keep being evaluated, and it must
    /// only contain options that do not change what the method does once the <c>await</c> is gone:
    /// <c>ConfigureAwaitOptions.SuppressThrowing</c> makes the method complete successfully when the awaited task
    /// faults, and <c>ConfigureAwaitOptions.ForceYielding</c> makes it complete asynchronously even when the awaited
    /// task is already completed.
    /// </remarks>
    internal static bool CanRemoveConfigureAwait(IInvocationOperation invocation, ITypeSymbol? configureAwaitOptionsSymbol)
    {
        if (invocation.Arguments.Length is not 1)
            return false;

        var argument = invocation.Arguments[0].Value;
        if (!argument.ConstantValue.HasValue)
            return false;

        return argument.ConstantValue.Value switch
        {
            // ConfigureAwait(bool) only configures where the continuation runs, and no continuation is left
            bool => true,

            // Any other option, including the ones a future version of .NET may add, can change what the method does
            int options when argument.Type.IsEqualTo(configureAwaitOptionsSymbol) => (options & ~ContinueOnCapturedContext) is 0,

            _ => false,
        };
    }
}
