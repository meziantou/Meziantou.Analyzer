using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes a syntax tree as an XML document, so it can be queried with XPath. Each syntax node is an element named
/// after its <see cref="SyntaxKind"/>, and the tokens of a node (<c>Identifier</c>, <c>Modifiers</c>, <c>Keyword</c>, ...)
/// are the attributes of its element, named after the property of the node that returns them.
/// </summary>
internal sealed class SyntaxNodeXPathNavigator : XPathNavigator
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TokenProperties = new();
    private static readonly ConcurrentDictionary<SyntaxKind, string> KindNames = new();

    private readonly SyntaxNode _root;
    private readonly XmlNameTable _nameTable;
    private readonly CancellationToken _cancellationToken;

    // null when the navigator is positioned on the document, which is the parent of the root node
    private SyntaxNode? _node;

    // The index of _node in the ChildNodesAndTokens of its parent, which allows moving to the siblings without searching the node.
    // It is -1 when it is not known yet, as moving to the parent does not require it.
    private int _indexInParent;
    private Attribute[]? _attributes;
    private int _attributeIndex = -1;

    public SyntaxNodeXPathNavigator(SyntaxNode root, CancellationToken cancellationToken)
    {
        _root = root;
        _nameTable = new NameTable();
        _cancellationToken = cancellationToken;
    }

    private SyntaxNodeXPathNavigator(SyntaxNodeXPathNavigator other)
    {
        _root = other._root;
        _nameTable = other._nameTable;
        _cancellationToken = other._cancellationToken;
        _node = other._node;
        _indexInParent = other._indexInParent;
        _attributes = other._attributes;
        _attributeIndex = other._attributeIndex;
    }

    /// <summary>
    /// The node the navigator is positioned on, or the node that owns the attribute the navigator is positioned on.
    /// </summary>
    public SyntaxNode? Node => _node;

    /// <summary>
    /// The name of the attribute the navigator is positioned on, if any.
    /// </summary>
    public string? AttributeName => IsOnAttribute ? _attributes![_attributeIndex].Name : null;

    /// <summary>
    /// The span of the node or of the tokens of the attribute the navigator is positioned on.
    /// </summary>
    public TextSpan Span => IsOnAttribute ? _attributes![_attributeIndex].Span : _node?.Span ?? _root.Span;

    private bool IsOnAttribute => _attributeIndex >= 0;

    public override XmlNameTable NameTable => _nameTable;

    public override XPathNodeType NodeType
    {
        get
        {
            if (IsOnAttribute)
                return XPathNodeType.Attribute;

            return _node is null ? XPathNodeType.Root : XPathNodeType.Element;
        }
    }

    public override string LocalName
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Name;

            return _node is null ? "" : GetKindName(_node.Kind());
        }
    }

    public override string Name => LocalName;

    public override string NamespaceURI => "";

    public override string Prefix => "";

    public override string BaseURI => "";

    public override bool IsEmptyElement => _node is not null && !IsOnAttribute && !HasChildNodes(_node);

    public override string Value
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Value;

            return (_node ?? _root).ToString();
        }
    }

    public override XPathNavigator Clone() => new SyntaxNodeXPathNavigator(this);

    public override bool IsSamePosition(XPathNavigator other)
    {
        return other is SyntaxNodeXPathNavigator navigator
            && navigator._root == _root
            && navigator._node == _node
            && navigator._attributeIndex == _attributeIndex;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        if (other is not SyntaxNodeXPathNavigator navigator || navigator._root != _root)
            return false;

        _node = navigator._node;
        _indexInParent = navigator._indexInParent;
        _attributes = navigator._attributes;
        _attributeIndex = navigator._attributeIndex;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        if (_node is null || IsOnAttribute)
            return false;

        _attributes ??= GetAttributes(_node);
        if (_attributes.Length is 0)
            return false;

        _attributeIndex = 0;
        return true;
    }

    public override bool MoveToNextAttribute()
    {
        if (!IsOnAttribute || _attributeIndex + 1 >= _attributes!.Length)
            return false;

        _attributeIndex++;
        return true;
    }

    public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope) => false;

    public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope) => false;

    public override bool MoveToId(string id) => false;

    public override bool MoveToFirstChild()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (IsOnAttribute)
            return false;

        if (_node is null)
        {
            SetNode(_root, indexInParent: 0);
            return true;
        }

        var index = FindChildNode(_node.ChildNodesAndTokens(), startIndex: 0, step: 1);
        if (index < 0)
            return false;

        SetNode(_node.ChildNodesAndTokens()[index].AsNode()!, index);
        return true;
    }

    public override bool MoveToNext() => MoveToSibling(step: 1);

    public override bool MoveToPrevious() => MoveToSibling(step: -1);

    public override bool MoveToParent()
    {
        if (IsOnAttribute)
        {
            _attributeIndex = -1;
            return true;
        }

        if (_node is null)
            return false;

        if (_node == _root)
        {
            _node = null;
            _indexInParent = 0;
            _attributes = null;
            return true;
        }

        var parent = _node.Parent!;
        SetNode(parent, parent == _root ? 0 : -1);
        return true;
    }

    public override void MoveToRoot()
    {
        _node = null;
        _indexInParent = 0;
        _attributes = null;
        _attributeIndex = -1;
    }

    private bool MoveToSibling(int step)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (IsOnAttribute || _node is null || _node == _root)
            return false;

        if (_indexInParent < 0)
        {
            _indexInParent = GetIndexInParent(_node);
        }

        var siblings = _node.Parent!.ChildNodesAndTokens();
        var index = FindChildNode(siblings, _indexInParent + step, step);
        if (index < 0)
            return false;

        SetNode(siblings[index].AsNode()!, index);
        return true;
    }

    private void SetNode(SyntaxNode node, int indexInParent)
    {
        _node = node;
        _indexInParent = indexInParent;
        _attributes = null;
        _attributeIndex = -1;
    }

    private static int FindChildNode(ChildSyntaxList children, int startIndex, int step)
    {
        for (var i = startIndex; i >= 0 && i < children.Count; i += step)
        {
            if (children[i].IsNode)
                return i;
        }

        return -1;
    }

    private static int GetIndexInParent(SyntaxNode node)
    {
        var siblings = node.Parent!.ChildNodesAndTokens();
        for (var i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].AsNode() == node)
                return i;
        }

        throw new InvalidOperationException("The node is not a child of its parent");
    }

    private static bool HasChildNodes(SyntaxNode node)
    {
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
                return true;
        }

        return false;
    }

    private static string GetKindName(SyntaxKind kind) => KindNames.GetOrAdd(kind, static kind => kind.ToString());

    private static Attribute[] GetAttributes(SyntaxNode node)
    {
        var properties = TokenProperties.GetOrAdd(node.GetType(), static type =>
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length is 0)
                .Where(property => property.PropertyType == typeof(SyntaxToken) || property.PropertyType == typeof(SyntaxTokenList))
                .Where(property => property.GetCustomAttribute<ObsoleteAttribute>() is null)
                .OrderBy(property => property.Name, StringComparer.Ordinal),
        ]);

        if (properties.Length is 0)
            return [];

        var attributes = new List<Attribute>(properties.Length);
        foreach (var property in properties)
        {
            object? value;
            try
            {
                value = property.GetValue(node);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            switch (value)
            {
                case SyntaxToken token when !token.IsMissing && !token.IsKind(SyntaxKind.None):
                    attributes.Add(new Attribute(property.Name, token.Text, token.Span));
                    break;

                case SyntaxTokenList { Count: > 0 } tokens:
                    attributes.Add(new Attribute(property.Name, string.Join(" ", tokens.Select(token => token.Text)), tokens.Span));
                    break;
            }
        }

        return [.. attributes];
    }

    private readonly record struct Attribute(string Name, string Value, TextSpan Span);
}
