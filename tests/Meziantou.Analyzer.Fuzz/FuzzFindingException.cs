namespace Meziantou.Analyzer.Fuzz;

/// <summary>Report a finding to libFuzzer as a crash, so libFuzzer can minimize its input.</summary>
internal sealed class FuzzFindingException : Exception
{
    public FuzzFindingException()
    {
    }

    public FuzzFindingException(string message)
        : base(message)
    {
    }

    public FuzzFindingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public FuzzFindingException(Finding finding)
        : base($"{finding.Title} ({finding.Signature})\n{finding.StackTrace}")
    {
    }
}
