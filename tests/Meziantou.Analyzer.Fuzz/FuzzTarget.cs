using System.Text;
using SharpFuzz;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// The function libFuzzer calls for each input. The findings are saved instead of being reported to libFuzzer as crashes,
/// as libFuzzer stops on the first crash. Only the inputs that kill the process or that do not complete stop libFuzzer.
/// </summary>
internal sealed class FuzzTarget
{
    private const int MaxInputLength = 64 * 1024;

    private readonly AnalysisEngine _engine;
    private readonly string? _targetSignature = FuzzEnvironment.TargetSignature;
    private readonly Meziantou.Framework.FullPath? _findingsDirectory = FuzzEnvironment.FindingsDirectory;
    private readonly Meziantou.Framework.FullPath? _currentInputFile = FuzzEnvironment.CurrentInputFile;

    private FuzzTarget()
    {
        _engine = new AnalysisEngine(ComponentFilter.Create(only: [], FuzzEnvironment.GetSuppressedComponents(), runCodeFixes: true));
    }

    public static void RunLibFuzzer()
    {
        // The instrumented assemblies must not run before libFuzzer.Run allocates the coverage buffer, which includes their module initializers
        FuzzTarget? target = null;
        Fuzzer.LibFuzzer.Run(data => (target ??= new FuzzTarget()).Execute(data));
    }

    private void Execute(ReadOnlySpan<byte> data)
    {
        if (data.Length > MaxInputLength)
            return;

        if (_currentInputFile is { } currentInputFile)
        {
            File.WriteAllBytes(currentInputFile, data);
        }

        var source = Encoding.UTF8.GetString(data);
        var findings = _engine.RunAsync(source).GetAwaiter().GetResult();
        foreach (var finding in findings)
        {
            if (_targetSignature is not null)
            {
                // Report the finding to libFuzzer, so it can minimize the input
                if (finding.Signature == _targetSignature)
                    throw new FuzzFindingException(finding);
            }
            else if (_findingsDirectory is { } findingsDirectory)
            {
                FindingStore.Save(findingsDirectory, finding, source);
            }
        }
    }
}
