using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Meziantou.Analyzer.Rules;

internal static partial class StringExtensions
{
#if NETSTANDARD2_0
    public static bool Contains(this string str, string value, StringComparison stringComparison)
    {
        return str.IndexOf(value, stringComparison) >= 0;
    }

    public static bool Contains(this string str, char value, StringComparison stringComparison)
    {
        return str.IndexOf(value, stringComparison) >= 0;
    }

    [SuppressMessage("Usage", "MA0001:StringComparison is missing", Justification = "Not needed")]
    public static int IndexOf(this string str, char value, StringComparison stringComparison)
    {
        if (stringComparison == StringComparison.Ordinal)
            return str.IndexOf(value);

        return str.IndexOf(value.ToString(), stringComparison);
    }
#endif

    public static string ReplaceOrdinal(this string str, string oldValue, string newValue)
    {
#if NET5_0_OR_GREATER
        return str.Replace(oldValue, newValue, StringComparison.Ordinal);
#else
        return str.Replace(oldValue, newValue);
#endif
    }
    public static LineSplitEnumerator SplitLines(this string str) => new(str.AsSpan());

    [StructLayout(LayoutKind.Auto)]
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        public readonly LineSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_str.Length == 0)
                return false;

            var span = _str;
            var index = span.IndexOfAny('\r', '\n');
            /* Unmerged change from project 'Meziantou.Analyzer (netstandard2.0)'
            Before:
                            if (index == -1)
                            {
                                _str = ReadOnlySpan<char>.Empty;
            After:
                            if (index == -1)
                        {
                            _str = ReadOnlySpan<char>.Empty;
            */

            if (index == -1)
            {
                _str = ReadOnlySpan<char>.Empty;
                Current = new LineSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            if (index < span.Length - 1 && span[index] == '\r')
            {
                var next = span[index + 1];
                if (next == '\n')
                {
                    Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 2));
                    _str = span.Slice(index + 2);
                    return true;
                }
            }

            Current = new LineSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _str = span.Slice(index + 1);
            return true;
        }

        public LineSplitEntry Current { get; private set; }
    }

    [StructLayout(LayoutKind.Auto)]
    public readonly ref struct LineSplitEntry
    {
        public LineSplitEntry(ReadOnlySpan<char> line, ReadOnlySpan<char> separator)
        {
            Line = line;
            Separator = separator;
        }

        public ReadOnlySpan<char> Line { get; }
        public ReadOnlySpan<char> Separator { get; }

        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Line;
            separator = Separator;
        }

        public static implicit operator ReadOnlySpan<char>(LineSplitEntry entry) => entry.Line;
    }
}
