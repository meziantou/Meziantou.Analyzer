using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TestHelper;

public sealed class DiagnosticResult
{
    private IReadOnlyList<DiagnosticResultLocation> _locations;

    public IReadOnlyList<DiagnosticResultLocation> Locations
    {
        get => _locations ??= [];
        set => _locations = value;
    }

    public DiagnosticSeverity? Severity { get; set; }

    public string Id { get; set; }

    public string Message { get; set; }

    public string Path => Locations.Count > 0 ? Locations[0].Path : "";

    public int Line => Locations.Count > 0 ? Locations[0].LineStart : -1;

    public int Column => Locations.Count > 0 ? Locations[0].ColumnStart : -1;
}
