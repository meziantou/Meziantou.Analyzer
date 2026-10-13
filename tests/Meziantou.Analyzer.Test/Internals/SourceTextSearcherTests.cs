using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class SourceTextSearcherTests
{
    private static List<int> FindAll(string text, string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        using var searcher = new SourceTextSearcher(SourceText.From(text), value, comparisonType);

        var result = new List<int>();
        var position = 0;
        while ((position = searcher.IndexOf(position)) >= 0)
        {
            result.Add(position);
            position++;
        }

        return result;
    }

    [Theory]
    [InlineData("", "ab")]
    [InlineData("a", "ab")]
    [InlineData("ba", "ab")]
    [InlineData("AB", "ab")]
    public void TheValueIsNotFound(string text, string value)
    {
        Assert.Empty(FindAll(text, value));
    }

    [Fact]
    public void AllTheOccurrencesAreFound()
    {
        Assert.Equal([0, 3, 4, 5, 8], FindAll("ab aaab ab", "a"));
        Assert.Equal([0, 5, 8], FindAll("ab aaab ab", "ab"));
        Assert.Equal([0, 1, 2], FindAll("aaaa", "aa"));
    }

    [Fact]
    public void TheComparisonTypeIsUsed()
    {
        Assert.Equal([0, 3, 6], FindAll("ab AB Ab", "ab", StringComparison.OrdinalIgnoreCase));
    }

    // The text is read by chunks, so the value can be at the end of a chunk, at the start of the next one, or span both
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheValueIsFoundAroundTheEndOfAReadBuffer(int bufferCount)
    {
        const string Value = "abcd";
        for (var offset = -(Value.Length * 2); offset <= Value.Length * 2; offset++)
        {
            var position = (bufferCount * SourceTextSearcher.ReadBufferSize) + offset;
            var text = new string(' ', position) + Value + "  " + Value;

            Assert.Equal([position, position + Value.Length + 2], FindAll(text, Value));
        }
    }

    [Fact]
    public void TheSearchCanStartAnywhere()
    {
        var text = "ab" + new string(' ', SourceTextSearcher.ReadBufferSize * 2) + "ab";
        var lastPosition = text.Length - 2;
        using var searcher = new SourceTextSearcher(SourceText.From(text), "ab", StringComparison.Ordinal);

        Assert.Equal(lastPosition, searcher.IndexOf(lastPosition));
        Assert.Equal(-1, searcher.IndexOf(lastPosition + 1));
        Assert.Equal(-1, searcher.IndexOf(text.Length));
        Assert.Equal(0, searcher.IndexOf(0));
        Assert.Equal(lastPosition, searcher.IndexOf(1));
    }
}
