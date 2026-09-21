namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The syntax tree of a file, with the data the navigators compute from it. It is built once per file and shared by
/// the navigator and all its clones, as an XPath evaluation clones the navigator for every step.
/// </summary>
internal sealed class SyntaxForest
{
    // The nodes of Roslyn do not override Equals and GetHashCode, so the comparison is reference equality. The forest
    // is used by a single analysis of a single file, which is not concurrent.
    private readonly Dictionary<SyntaxNode, XPathAttribute[]> _attributes = new(ReferenceComparer<SyntaxNode>.Instance);

    public SyntaxForest(SyntaxNode root, SemanticModel? semanticModel, XPathAttributeFilter filter, CancellationToken cancellationToken)
    {
        Root = root;
        SemanticModel = semanticModel;
        Selection = new SyntaxNodeXPathNavigator.AttributeSelection(filter);
        CancellationToken = cancellationToken;
    }

    /// <summary>The node the document of the queries is built from.</summary>
    public SyntaxNode Root { get; }

    /// <summary>The semantic model of the file, when the queries expose the attributes of the <c>semantic</c> namespace.</summary>
    public SemanticModel? SemanticModel { get; }

    /// <summary>The attributes the queries of the file can select.</summary>
    public SyntaxNodeXPathNavigator.AttributeSelection Selection { get; }

    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// The attributes of a node. They are computed once per file, as a query that tests an attribute visits the same
    /// node several times, and a file usually has several queries.
    /// </summary>
    public XPathAttribute[] GetAttributes(SyntaxNode node)
    {
        if (_attributes.TryGetValue(node, out var attributes))
            return attributes;

        attributes = SyntaxNodeXPathNavigator.BuildAttributes(node, this);
        _attributes.Add(node, attributes);
        return attributes;
    }
}
