using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticInvocationReportOptions
{
    None = 0x0,
    ReportOnMember = 0x1,
}
