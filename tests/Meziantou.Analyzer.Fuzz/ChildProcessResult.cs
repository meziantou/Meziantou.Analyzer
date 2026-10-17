namespace Meziantou.Analyzer.Fuzz;

internal sealed record ChildProcessResult(int? ExitCode, bool TimedOut, string Output)
{
    /// <summary>The harness exits with 0 when there is no finding and 1 when there are findings, any other exit code is a crash.</summary>
    public bool IsHarnessCrash => !TimedOut && ExitCode is not (0 or 1);
}
