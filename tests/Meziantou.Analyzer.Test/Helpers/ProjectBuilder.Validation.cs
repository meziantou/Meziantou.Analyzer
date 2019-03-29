using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHelper
{
    public partial class ProjectBuilder
    {
        public ProjectBuilder WithAnalyzer(DiagnosticAnalyzer diagnosticAnalyzer)
        {
            return this;
        }

        public ProjectBuilder WithCodeFixProvider(CodeFixProvider codeFixProvider)
        {
            return this;
        }

        public ProjectBuilder ShouldReportDiagnostic(int line, int column, int id, string message, DiagnosticSeverity severity)
        {
            return this;
        }

        public ProjectBuilder ShouldReportDiagnostic(params DiagnosticResult[] diagnosticResults)
        {
            return this;
        }

        public ProjectBuilder ShouldFix(string expectedCode)
        {
            return this;
        }
    }
}

