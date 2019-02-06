using Microsoft.CodeAnalysis;
using System;
using System.Runtime.InteropServices;

namespace TestHelper
{
    /// <summary>
    /// Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[] _locations;

        public DiagnosticResultLocation[] Locations
        {
            get => _locations ?? (_locations = Array.Empty<DiagnosticResultLocation>());
            set => _locations = value;
        }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path => Locations.Length > 0 ? Locations[0].Path : "";

        public int Line => Locations.Length > 0 ? Locations[0].Line : -1;

        public int Column => Locations.Length > 0 ? Locations[0].Column : -1;
    }
}
