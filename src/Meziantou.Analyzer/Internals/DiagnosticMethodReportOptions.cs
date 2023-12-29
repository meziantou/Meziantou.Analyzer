using System;

namespace Meziantou.Analyzer;

[Flags]
public enum DiagnosticMethodReportOptions
{
    None = 0x0,
    ReportOnMethodName = 0x1,
    ReportOnReturnType = 0x2,
}
