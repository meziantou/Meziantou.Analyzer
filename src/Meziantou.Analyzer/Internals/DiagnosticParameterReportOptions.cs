using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticParameterReportOptions
{
    None = 0x0,
    ReportOnType = 0x1,
}
