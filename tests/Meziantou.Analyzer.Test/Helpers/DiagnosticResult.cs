using Microsoft.CodeAnalysis;
using System;

namespace TestHelper
{
    public class DiagnosticResult
    {
        private DiagnosticResultLocation[] _locations;

        public DiagnosticResultLocation[] Locations
        {
            get => _locations ?? (_locations = Array.Empty<DiagnosticResultLocation>());
            set => _locations = value;
        }

        public DiagnosticSeverity? Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path => Locations.Length > 0 ? Locations[0].Path : "";

        public int Line => Locations.Length > 0 ? Locations[0].Line : -1;

        public int Column => Locations.Length > 0 ? Locations[0].Column : -1;
    }
}
