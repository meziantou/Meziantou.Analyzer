using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticReportOptions
{
    None = 0x0,
    ReportOnMethodName = 0x1,
}
