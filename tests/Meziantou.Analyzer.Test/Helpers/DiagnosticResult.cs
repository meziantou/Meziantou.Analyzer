using System;
using Microsoft.CodeAnalysis;

namespace TestHelper
{
    public sealed class DiagnosticResult
    {
        private DiagnosticResultLocation[] _locations;

        public DiagnosticResultLocation[] Locations
        {
            get => _locations ??= Array.Empty<DiagnosticResultLocation>();
            set => _locations = value;
        }

        public DiagnosticSeverity? Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path => Locations.Length > 0 ? Locations[0].Path : "";

        public int Line => Locations.Length > 0 ? Locations[0].LineStart : -1;

        public int Column => Locations.Length > 0 ? Locations[0].ColumnStart : -1;
    }
}
