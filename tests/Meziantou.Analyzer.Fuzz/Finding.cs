using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// A defect found by the fuzzer. The findings with the same <see cref="Signature"/> are the same defect.
/// </summary>
internal sealed class Finding
{
    private const int SignatureFrameCount = 5;

    public required FindingKind Kind { get; init; }

    /// <summary>The full name of the analyzer or of the code fixer.</summary>
    public required string Component { get; init; }

    /// <summary>The rules of the analyzer, or the rule whose diagnostic the code fixer fixes.</summary>
    public ImmutableArray<string> DiagnosticIds { get; init; } = [];

    public string? ExceptionType { get; init; }

    public string? Message { get; init; }

    public string? StackTrace { get; init; }

    /// <summary>The frames of the analyzer assemblies, normalized so they do not depend on the compiler-generated names.</summary>
    public ImmutableArray<string> Frames { get; init; } = [];

    /// <summary>The errors that the code fix introduces.</summary>
    public ImmutableArray<string> CompilerErrors { get; init; } = [];

    public string? CodeActionTitle { get; init; }

    /// <summary>The code after the code fix is applied.</summary>
    public string? FixedSource { get; init; }

    public string Signature => field ??= ComputeSignature();

    public string Title => field ??= ComputeTitle();

    public static Finding FromException(FindingKind kind, object component, ImmutableArray<string> diagnosticIds, Exception exception)
    {
        while (exception is AggregateException { InnerExceptions.Count: 1 } or TargetInvocationException { InnerException: not null })
        {
            exception = exception.InnerException!;
        }

        var text = exception.ToString();
        var frames = StackTraceParser.GetAnalyzerFrames(text);
        return new Finding
        {
            Kind = frames.IsEmpty && kind is FindingKind.AnalyzerException or FindingKind.CodeFixException ? FindingKind.RoslynException : kind,
            Component = component as string ?? Components.GetName(component),
            DiagnosticIds = diagnosticIds,
            ExceptionType = exception.GetType().FullName,
            Message = exception.Message,
            StackTrace = text,
            Frames = frames,
        };
    }

    private string ComputeSignature()
    {
        IEnumerable<string> parts = Kind switch
        {
            FindingKind.CodeFixBreaksCode => [Kind.ToString(), Component, .. DiagnosticIds, .. CompilerErrors],
            FindingKind.CodeFixDoesNotChangeCode or FindingKind.Timeout => [Kind.ToString(), Component, .. DiagnosticIds],
            _ => [Kind.ToString(), Component, ExceptionType ?? "", .. Frames.Take(SignatureFrameCount)],
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', parts)));
        return Convert.ToHexStringLower(hash)[..16];
    }

    private string ComputeTitle()
    {
        var rules = DiagnosticIds.Length switch
        {
            0 => "",
            <= 3 => string.Join(", ", DiagnosticIds) + " ",
            _ => string.Join(", ", DiagnosticIds.Take(3)) + ", … ",
        };

        var component = GetShortName(Component);
        var location = Frames.IsEmpty ? "" : " in " + GetShortFrame(Frames[0]);
        var description = Kind switch
        {
            FindingKind.AnalyzerException or FindingKind.CodeFixException or FindingKind.RoslynException or FindingKind.HarnessException => $"throws {GetShortName(ExceptionType ?? "an exception")}{location}",
            FindingKind.CodeFixBreaksCode => $"code fix introduces {string.Join(", ", CompilerErrors)}",
            FindingKind.CodeFixDoesNotChangeCode => "registers a code fix that does not change the code",
            FindingKind.Crash => $"crashes the process{(Message is null ? "" : ": " + Message)}{location}",
            FindingKind.Timeout => "does not complete",
            _ => Kind.ToString(),
        };

        return $"[Fuzzing] {rules}({component}) {description}";
    }

    private static string GetShortName(string name) => name[(name.LastIndexOfAny(['.', '+']) + 1)..];

    private static string GetShortFrame(string frame)
    {
        var index = frame.AsSpan().LastIndexOf('.');
        if (index > 0)
        {
            index = frame.AsSpan(0, index).LastIndexOf('.');
        }

        return frame[(index + 1)..];
    }
}
