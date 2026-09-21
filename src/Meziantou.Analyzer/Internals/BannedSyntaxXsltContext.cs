using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The context of the XPath queries of the banned syntax files. It defines the <c>semantic</c> and <c>operation</c>
/// prefixes, and the <c>syntax</c> function, which returns the syntax nodes of the operations it is given, so a query
/// on the operations can continue on the syntax tree.
/// </summary>
internal sealed class BannedSyntaxXsltContext : XsltContext
{
    /// <summary>The name of the function that returns the syntax nodes of a set of operations.</summary>
    public const string SyntaxFunctionName = "syntax";

    /// <summary>
    /// A context whose <c>syntax</c> function returns nothing, which is used to validate a query without a compilation.
    /// </summary>
    public static readonly BannedSyntaxXsltContext Empty = new(syntaxNavigatorFactory: null);

    private readonly Func<SyntaxNodeXPathNavigator>? _syntaxNavigatorFactory;

    public BannedSyntaxXsltContext(Func<SyntaxNodeXPathNavigator>? syntaxNavigatorFactory)
        : base(new NameTable())
    {
        // The context resolves the prefixes when the expression is evaluated, so it defines the same ones as the
        // resolver that compiles it
        AddNamespace(XPathNamespaces.SemanticPrefix, XPathNamespaces.SemanticNamespaceUri);
        AddNamespace(XPathNamespaces.OperationPrefix, XPathNamespaces.OperationNamespaceUri);
        _syntaxNavigatorFactory = syntaxNavigatorFactory;
    }

    public override bool Whitespace => false;

    public override bool PreserveWhitespace(XPathNavigator node) => false;

    public override int CompareDocument(string baseUri, string nextbaseUri) => string.CompareOrdinal(baseUri, nextbaseUri);

    // The queries have no variable. The base type is not annotated, and null is how it says that a name is unknown.
    public override IXsltContextVariable ResolveVariable(string prefix, string name) => null!;

    // An unknown function is reported, as the engine throws when it cannot be resolved
    public override IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] argTypes)
    {
        if (prefix.Length is 0 && string.Equals(name, SyntaxFunctionName, StringComparison.Ordinal))
            return new SyntaxFunction(_syntaxNavigatorFactory);

        return null!;
    }

    private sealed class SyntaxFunction(Func<SyntaxNodeXPathNavigator>? syntaxNavigatorFactory) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.NodeSet;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.NodeSet];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (syntaxNavigatorFactory is null || args is not [XPathNodeIterator operations])
                return SyntaxNodeIterator.Empty;

            // Several operations can share the same syntax node, such as an expression and the conversion that wraps it
            var nodes = new List<SyntaxNode>();
            var visited = new HashSet<SyntaxNode>();
            while (operations.MoveNext())
            {
                if (operations.Current is OperationXPathNavigator { Operation: { } operation } && visited.Add(operation.Syntax))
                {
                    nodes.Add(operation.Syntax);
                }
            }

            return nodes.Count is 0 ? SyntaxNodeIterator.Empty : new SyntaxNodeIterator(syntaxNavigatorFactory(), nodes, index: -1);
        }
    }

    private sealed class SyntaxNodeIterator : XPathNodeIterator
    {
        public static readonly SyntaxNodeIterator Empty = new(navigator: null, [], index: -1);

        private readonly SyntaxNodeXPathNavigator? _navigator;
        private readonly List<SyntaxNode> _nodes;
        private SyntaxNodeXPathNavigator? _current;
        private int _index;

        public SyntaxNodeIterator(SyntaxNodeXPathNavigator? navigator, List<SyntaxNode> nodes, int index)
        {
            _navigator = navigator;
            _nodes = nodes;
            _index = index;
            if (index >= 0 && index < nodes.Count)
            {
                _current = navigator!.CreateAt(nodes[index]);
            }
        }

        // The engine reads the navigator of the current position, so it is the same instance that is moved
        public override XPathNavigator? Current => _current;

        public override int CurrentPosition => _index + 1;

        public override int Count => _nodes.Count;

        public override XPathNodeIterator Clone() => new SyntaxNodeIterator(_navigator, _nodes, _index);

        public override bool MoveNext()
        {
            if (_index + 1 >= _nodes.Count)
                return false;

            _index++;
            if (_current is null)
            {
                _current = _navigator!.CreateAt(_nodes[_index]);
            }
            else
            {
                _current.MoveToNode(_nodes[_index]);
            }

            return true;
        }
    }
}
