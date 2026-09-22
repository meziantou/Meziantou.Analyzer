using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes the symbols declared in a file as an XML document, so they can be queried with XPath. Each symbol is an
/// element of the <c>symbol</c> namespace named after its <see cref="SymbolKind"/>, such as <c>symbol:NamedType</c>
/// or <c>symbol:Method</c>, and the children of an element are the symbols it contains. The properties of the symbol
/// are the attributes of its element, named after the property, such as <c>IsStatic</c> or <c>ReturnType</c>.
/// </summary>
internal sealed class SymbolXPathNavigator : XPathNavigator, IBannedSyntaxNavigator
{
    // The kinds of the symbols a forest contains
    private static readonly Dictionary<SymbolKind, string> KindNames = new()
    {
        [SymbolKind.Namespace] = nameof(SymbolKind.Namespace),
        [SymbolKind.NamedType] = nameof(SymbolKind.NamedType),
        [SymbolKind.Method] = nameof(SymbolKind.Method),
        [SymbolKind.Property] = nameof(SymbolKind.Property),
        [SymbolKind.Field] = nameof(SymbolKind.Field),
        [SymbolKind.Event] = nameof(SymbolKind.Event),
        [SymbolKind.Parameter] = nameof(SymbolKind.Parameter),
        [SymbolKind.TypeParameter] = nameof(SymbolKind.TypeParameter),
        [SymbolKind.Local] = nameof(SymbolKind.Local),
    };

    private static readonly Dictionary<SymbolKind, string> ElementNames = KindNames.ToDictionary(pair => pair.Key, pair => XPathNamespaces.SymbolPrefix + ":" + pair.Value);
    private static readonly HashSet<string> KindNameSet = new(KindNames.Values, StringComparer.Ordinal);

    // The members that are not attributes: the kind is the name of the element, the parameters are the child elements,
    // the names are formatted like the other attributes that expose a symbol, and the other ones are about the
    // compilation or the syntax rather than the symbol
    private static readonly XPathPropertyAttributes PropertyAttributes = new(typeof(ISymbol), new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(ISymbol.Kind),
        nameof(ISymbol.Name),
        nameof(ISymbol.MetadataName),
        nameof(ISymbol.Locations),
        nameof(ISymbol.DeclaringSyntaxReferences),
        nameof(ISymbol.ContainingAssembly),
        nameof(ISymbol.ContainingModule),
        nameof(ISymbol.OriginalDefinition),
        nameof(ISymbol.Language),
        nameof(ISymbol.MetadataToken),
        nameof(IMethodSymbol.Parameters),
        nameof(IMethodSymbol.TypeParameters),
    });

    private readonly SymbolForest _forest;
    private readonly XmlNameTable _nameTable;

    // null when the navigator is positioned on the document, which is the parent of the roots
    private SymbolElement? _element;
    private XPathAttribute[]? _attributes;
    private int _attributeIndex = -1;

    public SymbolXPathNavigator(SymbolForest forest)
    {
        _forest = forest;
        _nameTable = new NameTable();
    }

    private SymbolXPathNavigator(SymbolXPathNavigator other)
    {
        _forest = other._forest;
        _nameTable = other._nameTable;
        _element = other._element;
        _attributes = other._attributes;
        _attributeIndex = other._attributeIndex;
    }

    /// <summary>
    /// The forest the navigator moves in.
    /// </summary>
    public SymbolForest Forest => _forest;

    /// <summary>
    /// The element the navigator is positioned on, or the element that owns the attribute it is positioned on.
    /// </summary>
    public SymbolElement? Element => _element;

    /// <summary>
    /// The name of the attribute the navigator is positioned on, if any.
    /// </summary>
    public string? AttributeName => IsOnAttribute ? _attributes![_attributeIndex].Name : null;

    /// <summary>
    /// The locations of the symbol in the file, which are also the ones of its attributes.
    /// </summary>
    public ImmutableArray<TextSpan> ReportSpans => _element is null ? [] : _forest.GetSpans(_element);

    /// <summary>
    /// The qualified name of the element, followed by the name of the attribute when the navigator is positioned on
    /// one, such as <c>symbol:Method/@Name</c>.
    /// </summary>
    public string? ReportName
    {
        get
        {
            if (_element is null)
                return null;

            var name = GetElementName(_element.Symbol.Kind);
            return AttributeName is { } attributeName ? name + "/@" + attributeName : name;
        }
    }

    private bool IsOnAttribute => _attributeIndex >= 0;

    public override XmlNameTable NameTable => _nameTable;

    public override XPathNodeType NodeType
    {
        get
        {
            if (IsOnAttribute)
                return XPathNodeType.Attribute;

            return _element is null ? XPathNodeType.Root : XPathNodeType.Element;
        }
    }

    public override string LocalName
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].LocalName;

            return _element is null ? "" : GetKindName(_element.Symbol.Kind);
        }
    }

    public override string Name
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Name;

            return _element is null ? "" : GetElementName(_element.Symbol.Kind);
        }
    }

    // The attributes are not in a namespace, so they are never prefixed
    public override string NamespaceURI => !IsOnAttribute && _element is not null ? XPathNamespaces.SymbolNamespaceUri : "";

    public override string Prefix => !IsOnAttribute && _element is not null ? XPathNamespaces.SymbolPrefix : "";

    public override string BaseURI => "";

    public override bool IsEmptyElement => _element is not null && !IsOnAttribute && _element.Children.Length is 0;

    public override string Value
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Value;

            return _element is null ? "" : _element.Symbol.ToDisplayString();
        }
    }

    public override XPathNavigator Clone() => new SymbolXPathNavigator(this);

    /// <summary>
    /// A navigator on the same forest, positioned on an element of it. It is how the <c>symbol</c> function returns
    /// the symbol of a syntax node.
    /// </summary>
    public SymbolXPathNavigator CreateAt(SymbolElement element)
    {
        var navigator = new SymbolXPathNavigator(this);
        navigator.SetElement(element);
        return navigator;
    }

    public override bool IsSamePosition(XPathNavigator other)
    {
        return other is SymbolXPathNavigator navigator
            && ReferenceEquals(navigator._forest, _forest)
            && ReferenceEquals(navigator._element, _element)
            && navigator._attributeIndex == _attributeIndex;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        if (other is not SymbolXPathNavigator navigator || !ReferenceEquals(navigator._forest, _forest))
            return false;

        _element = navigator._element;
        _attributes = navigator._attributes;
        _attributeIndex = navigator._attributeIndex;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        if (_element is null || IsOnAttribute)
            return false;

        _attributes ??= _forest.GetAttributes(_element);
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
        _forest.CancellationToken.ThrowIfCancellationRequested();
        if (IsOnAttribute)
            return false;

        var children = _element is null ? _forest.Roots : _element.Children;
        if (children.Length is 0)
            return false;

        SetElement(children[0]);
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

        if (_element is null)
            return false;

        if (_element.Parent is not { } parent)
        {
            MoveToRoot();
            return true;
        }

        SetElement(parent);
        return true;
    }

    public override void MoveToRoot()
    {
        _element = null;
        _attributes = null;
        _attributeIndex = -1;
    }

    private bool MoveToSibling(int step)
    {
        _forest.CancellationToken.ThrowIfCancellationRequested();
        if (IsOnAttribute || _element is null)
            return false;

        // The roots are the children of the document, so they are siblings of each other
        var siblings = _element.Parent is { } parent ? parent.Children : _forest.Roots;
        var index = _element.IndexInParent + step;
        if (index < 0 || index >= siblings.Length)
            return false;

        SetElement(siblings[index]);
        return true;
    }

    private void SetElement(SymbolElement element)
    {
        _element = element;
        _attributes = null;
        _attributeIndex = -1;
    }

    /// <summary>
    /// Indicates whether a name is the one of a kind of symbol a forest can contain, as the elements are named.
    /// </summary>
    public static bool IsKindName(string name) => KindNameSet.Contains(name);

    private static string GetKindName(SymbolKind kind) => KindNames.TryGetValue(kind, out var name) ? name : XPathAttributeFormatter.GetSymbolKindName(kind);

    private static string GetElementName(SymbolKind kind) => ElementNames.TryGetValue(kind, out var name) ? name : XPathNamespaces.SymbolPrefix + ":" + GetKindName(kind);

    /// <summary>
    /// The attributes of a symbol. It is <see cref="SymbolForest.GetAttributes(SymbolElement)"/> that calls it, so
    /// they are computed once per file. The name of the symbol is formatted like the other attributes that expose a
    /// symbol, and the other attributes are its properties.
    /// </summary>
    internal static XPathAttribute[] BuildAttributes(ISymbol symbol, ImmutableArray<TextSpan> spans, XPathAttributeFilter filter)
    {
        var attributes = new List<XPathAttribute>();
        var writer = new XPathAttributeWriter(attributes, namespaceUri: "", qualifiedNames: null, filter);

        // The attributes are reported on the locations of their element, so their own span is only informative
        var span = spans.IsDefaultOrEmpty ? default : spans[0];
        writer.Add(span, IdentityName.Name, symbol.Name);

        if (writer.Includes(IdentityName.QualifiedName))
        {
            writer.Add(span, IdentityName.QualifiedName, SymbolNameFormatter.GetSymbolName(symbol));
        }

        if (writer.Includes(IdentityName.DocumentationId))
        {
            writer.Add(span, IdentityName.DocumentationId, SymbolNameFormatter.GetDocumentationId(symbol));
        }

        if (symbol is ITypeSymbol type)
        {
            if (writer.Includes(IdentityName.MetadataName))
            {
                writer.Add(span, IdentityName.MetadataName, SymbolNameFormatter.GetMetadataName(type));
            }

            if (writer.Includes(IdentityName.ReferenceId))
            {
                writer.Add(span, IdentityName.ReferenceId, SymbolNameFormatter.GetReferenceId(type));
            }
        }

        PropertyAttributes.Add(writer, span, symbol);
        return attributes.Count is 0 ? [] : [.. attributes];
    }

    private static class IdentityName
    {
        public const string Name = nameof(Name);
        public const string QualifiedName = nameof(QualifiedName);
        public const string DocumentationId = nameof(DocumentationId);
        public const string MetadataName = nameof(MetadataName);
        public const string ReferenceId = nameof(ReferenceId);
    }
}
