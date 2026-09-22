using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The context of the XPath queries of the banned syntax files. It defines the <c>semantic</c>, <c>operation</c> and
/// <c>symbol</c> prefixes, the <c>syntax</c> function, which returns the syntax nodes of the operations or of the
/// symbols it is given, and the <c>symbol</c> function, which returns the symbols of the syntax nodes it is given.
/// </summary>
internal sealed class BannedSyntaxXsltContext : XsltContext
{
    /// <summary>The name of the function that returns the syntax nodes of a set of operations or of symbols.</summary>
    public const string SyntaxFunctionName = "syntax";

    /// <summary>The name of the function that returns the symbols of a set of syntax nodes.</summary>
    public const string SymbolFunctionName = "symbol";

    /// <summary>
    /// A context whose functions return nothing, which is used to validate a query without a compilation.
    /// </summary>
    public static readonly BannedSyntaxXsltContext Empty = new(syntaxNavigatorFactory: null, symbolNavigatorFactory: null);

    private readonly Func<SyntaxNodeXPathNavigator>? _syntaxNavigatorFactory;
    private readonly Func<SymbolXPathNavigator>? _symbolNavigatorFactory;

    public BannedSyntaxXsltContext(Func<SyntaxNodeXPathNavigator>? syntaxNavigatorFactory, Func<SymbolXPathNavigator>? symbolNavigatorFactory)
        : base(new NameTable())
    {
        // The context resolves the prefixes when the expression is evaluated, so it defines the same ones as the
        // resolver that compiles it
        AddNamespace(XPathNamespaces.SemanticPrefix, XPathNamespaces.SemanticNamespaceUri);
        AddNamespace(XPathNamespaces.OperationPrefix, XPathNamespaces.OperationNamespaceUri);
        AddNamespace(XPathNamespaces.SymbolPrefix, XPathNamespaces.SymbolNamespaceUri);
        _syntaxNavigatorFactory = syntaxNavigatorFactory;
        _symbolNavigatorFactory = symbolNavigatorFactory;
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

        if (prefix.Length is 0 && string.Equals(name, SymbolFunctionName, StringComparison.Ordinal))
            return new SymbolFunction(_symbolNavigatorFactory);

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
            if (syntaxNavigatorFactory is null || args is not [XPathNodeIterator items])
                return NavigatorIterator.Empty;

            // Several operations can share the same syntax node, such as an expression and the conversion that wraps
            // it, and a partial symbol has several syntax nodes
            var nodes = new List<SyntaxNode>();
            var visited = new HashSet<SyntaxNode>();
            while (items.MoveNext())
            {
                switch (items.Current)
                {
                    case OperationXPathNavigator { Operation: { } operation }:
                        if (visited.Add(operation.Syntax))
                        {
                            nodes.Add(operation.Syntax);
                        }

                        break;

                    case SymbolXPathNavigator { Element: { } element } navigator:
                        foreach (var node in navigator.Forest.GetSyntaxNodes(element))
                        {
                            if (visited.Add(node))
                            {
                                nodes.Add(node);
                            }
                        }

                        break;
                }
            }

            if (nodes.Count is 0)
                return NavigatorIterator.Empty;

            var syntaxNavigator = syntaxNavigatorFactory();
            var navigators = new List<XPathNavigator>(nodes.Count);
            foreach (var node in nodes)
            {
                navigators.Add(syntaxNavigator.CreateAt(node));
            }

            return new NavigatorIterator(navigators, index: -1);
        }
    }

    private sealed class SymbolFunction(Func<SymbolXPathNavigator>? symbolNavigatorFactory) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.NodeSet;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.NodeSet];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (symbolNavigatorFactory is null || args is not [XPathNodeIterator items])
                return NavigatorIterator.Empty;

            SymbolXPathNavigator? symbolNavigator = null;
            List<XPathNavigator>? navigators = null;

            // Several nodes can declare or refer to the same symbol, such as the parts of a partial type
            var visited = new HashSet<SymbolElement>();
            while (items.MoveNext())
            {
                if (items.Current is not SyntaxNodeXPathNavigator { Node: { } node })
                    continue;

                symbolNavigator ??= symbolNavigatorFactory();
                if (symbolNavigator.Forest.FindElement(node) is { } element && visited.Add(element))
                {
                    navigators ??= [];
                    navigators.Add(symbolNavigator.CreateAt(element));
                }
            }

            return navigators is null ? NavigatorIterator.Empty : new NavigatorIterator(navigators, index: -1);
        }
    }

    // The navigators of the list are never moved, as each iterator exposes its own copy of the current one
    private sealed class NavigatorIterator : XPathNodeIterator
    {
        public static readonly NavigatorIterator Empty = new([], index: -1);

        private readonly List<XPathNavigator> _navigators;
        private XPathNavigator? _current;
        private int _index;

        public NavigatorIterator(List<XPathNavigator> navigators, int index)
        {
            _navigators = navigators;
            _index = index;
            if (index >= 0 && index < navigators.Count)
            {
                _current = navigators[index].Clone();
            }
        }

        public override XPathNavigator? Current => _current;

        public override int CurrentPosition => _index + 1;

        public override int Count => _navigators.Count;

        public override XPathNodeIterator Clone() => new NavigatorIterator(_navigators, _index);

        public override bool MoveNext()
        {
            if (_index + 1 >= _navigators.Count)
                return false;

            _index++;
            _current = _navigators[_index].Clone();
            return true;
        }
    }
}
