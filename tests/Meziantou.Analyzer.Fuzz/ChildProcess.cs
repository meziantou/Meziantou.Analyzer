using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Meziantou.Analyzer.Fuzz;

internal static class ChildProcess
{
    /// <summary>
    /// Run the harness in a new process, so a stack overflow or a hang does not stop the current process.
    /// </summary>
    public static Task<ChildProcessResult> RunHarnessAsync(IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var (fileName, prefix) = GetHarnessCommand();
        return RunAsync(fileName, [.. prefix, .. arguments], environment: null, outputFile: null, timeout, isolateProcessGroup: false, cancellationToken);
    }

    /// <summary>The command that starts the harness: the dotnet host and the harness assembly, or the apphost.</summary>
    public static (string FileName, string[] Arguments) GetHarnessCommand()
    {
        var processPath = Environment.ProcessPath!;
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            return (processPath, [typeof(ChildProcess).Assembly.Location]);

        return (processPath, []);
    }

    /// <summary>
    /// Run a process until it exits or the timeout expires, then kill what remains of it.
    /// </summary>
    /// <param name="isolateProcessGroup">
    /// Start the process in a new process group (Linux only), and kill the whole group when the process exits. libFuzzer does not stop the harness
    /// when an input times out, so the harness keeps running the input forever, and keeps the redirected output open, after libFuzzer exits.
    /// </param>
    public static async Task<ChildProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, IReadOnlyDictionary<string, string?>? environment, string? outputFile, TimeSpan timeout, bool isolateProcessGroup, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(isolateProcessGroup ? "setsid" : fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        if (isolateProcessGroup)
        {
            // setsid does not fork, as the processes started by .NET are not process group leaders,
            // so the id of the process is the id of the new process group
            startInfo.ArgumentList.Add(fileName);
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (name, value) in environment)
            {
                startInfo.Environment[name] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var output = new StringBuilder();
        var outputLock = new Lock();
        await using var writer = outputFile is null ? null : new StreamWriter(outputFile, append: false, Encoding.UTF8) { AutoFlush = true };
        void OnData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
                return;

            lock (outputLock)
            {
                // Only the beginning and the end of the output of libFuzzer matter, so the output in memory is bounded
                if (output.Length < 1_000_000)
                {
                    output.AppendLine(e.Data);
                }

                writer?.WriteLine(e.Data);
            }
        }

        // Unlike WaitForExitAsync, the Exited event does not wait for the end of the redirected output
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exited.TrySetResult();
        process.OutputDataReceived += OnData;
        process.ErrorDataReceived += OnData;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timedOut = false;
        try
        {
            await exited.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            timedOut = true;
        }
        finally
        {
            if (isolateProcessGroup)
            {
                KillProcessGroup(process.Id);
            }
            else
            {
                KillProcessTree(process);
            }
        }

        // The redirected output ends when all the processes that inherited it exit
        try
        {
            await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }

        lock (outputLock)
        {
            return new ChildProcessResult(timedOut ? null : process.ExitCode, timedOut, output.ToString());
        }
    }

    public static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // The process already exited
        }
    }

    private static void KillProcessGroup(int processGroupId)
    {
        // A negative id sends the signal to all the processes of the group
        var startInfo = new ProcessStartInfo("kill") { UseShellExecute = false };
        startInfo.ArgumentList.Add("-KILL");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add((-processGroupId).ToString(CultureInfo.InvariantCulture));
        using var process = Process.Start(startInfo)!;
        process.WaitForExit();
    }
}
