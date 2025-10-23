using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Test.Helpers;

public sealed class DiagnosticResult
{
    public IReadOnlyList<DiagnosticResultLocation> Locations
    {
        get => field ??= [];
        set;
    }

    public DiagnosticSeverity? Severity { get; set; }

    public string? Id { get; set; }

    public string? Message { get; set; }

    public string Path => Locations.Count > 0 ? Locations[0].Path : "";

    public int Line => Locations.Count > 0 ? Locations[0].LineStart : -1;

    public int Column => Locations.Count > 0 ? Locations[0].ColumnStart : -1;
}
