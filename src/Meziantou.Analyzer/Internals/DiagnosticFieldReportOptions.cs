using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticFieldReportOptions
{
    None = 0x0,
    ReportOnReturnType = 0x1,
}
