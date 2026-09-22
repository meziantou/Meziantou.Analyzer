using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// A symbol of a <see cref="SymbolForest"/>, with its position in the tree.
/// </summary>
internal sealed class SymbolElement(ISymbol symbol, int position)
{
    public ISymbol Symbol { get; } = symbol;

    /// <summary>The start of the first location of the symbol in the file, which orders the elements.</summary>
    public int Position { get; } = position;

    public SymbolElement? Parent { get; set; }

    public SymbolElement[] Children { get; set; } = [];

    public int IndexInParent { get; set; }

    internal ImmutableArray<TextSpan> Spans { get; set; }

    internal XPathAttribute[]? Attributes { get; set; }
}
