using System;
using System.Runtime.InteropServices;

namespace TestHelper;

/// <summary>
/// Location where the diagnostic appears, as determined by path, line number, and column number.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct DiagnosticResultLocation
{
    public DiagnosticResultLocation(string path, int lineStart, int columnStart, int lineEnd, int columnEnd)
    {
        if (lineStart < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(lineStart), "line must be >= -1");
        }

        if (columnStart < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(columnStart), "column must be >= -1");
        }

        Path = path;
        LineStart = lineStart;
        ColumnStart = columnStart;
        LineEnd = lineEnd;
        ColumnEnd = columnEnd;
    }

    public string Path { get; }
    public int LineStart { get; }
    public int ColumnStart { get; }
    public int LineEnd { get; }
    public int ColumnEnd { get; }

    public bool IsSpan => LineStart != LineEnd || ColumnStart != ColumnEnd;
}
