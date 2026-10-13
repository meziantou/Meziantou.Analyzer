using System.Buffers;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Searches a value in a <see cref="SourceText"/> without allocating the text of the file. The text is read by
/// chunks, and the search in a chunk is vectorized on .NET, so this is much faster than enumerating the tokens or
/// the trivia of a file to find the few places that need to be analyzed.
/// </summary>
internal sealed class SourceTextSearcher : IDisposable
{
    /// <summary>The number of characters read at once.</summary>
    internal const int ReadBufferSize = 4096;

    private readonly SourceText _text;
    private readonly string _value;
    private readonly StringComparison _comparisonType;
    private char[]? _buffer;
    private int _bufferStart;
    private int _bufferLength;

    public SourceTextSearcher(SourceText text, string value, StringComparison comparisonType)
    {
        Debug.Assert(value.Length is > 0 and <= ReadBufferSize);

        _text = text;
        _value = value;
        _comparisonType = comparisonType;
    }

    /// <summary>
    /// Returns the position of the first occurrence of the value that starts at <paramref name="startIndex"/> or
    /// after it, or -1 when there is none.
    /// </summary>
    public int IndexOf(int startIndex)
    {
        while (_text.Length - startIndex >= _value.Length)
        {
            var offset = startIndex - _bufferStart;
            if (_buffer is null || offset < 0 || _bufferLength - offset < _value.Length)
            {
                _buffer ??= ArrayPool<char>.Shared.Rent(ReadBufferSize);
                _bufferStart = startIndex;
                _bufferLength = Math.Min(ReadBufferSize, _text.Length - startIndex);
                _text.CopyTo(startIndex, _buffer, 0, _bufferLength);
                offset = 0;
            }

            var index = new ReadOnlySpan<char>(_buffer, offset, _bufferLength - offset).IndexOf(_value.AsSpan(), _comparisonType);
            if (index >= 0)
                return startIndex + index;

            // Read the last characters again, as the value can span two buffers
            startIndex = _bufferStart + _bufferLength - (_value.Length - 1);
        }

        return -1;
    }

    public void Dispose()
    {
        if (_buffer is not null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}
