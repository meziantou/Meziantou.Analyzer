using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticPropertyReportOptions
{
    None = 0x0,
    ReportOnReturnType = 0x1,
}
