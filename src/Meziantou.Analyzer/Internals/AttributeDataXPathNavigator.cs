using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes the attributes applied to a symbol as an XML document, so the <c>attributes</c> function can return them.
/// Each attribute is an <c>AttributeData</c> element, whose children are its <c>ConstructorArgument</c> and
/// <c>NamedArgument</c> elements. The items of an argument that is an array are its <c>Item</c> child elements.
/// </summary>
internal sealed class AttributeDataXPathNavigator : XPathNavigator, IBannedSyntaxNavigator
{
    private readonly AttributeDataForest _forest;
    private readonly XmlNameTable _nameTable;

    // null when the navigator is positioned on the document, which is the parent of the roots
    private AttributeDataElement? _element;
    private XPathAttribute[]? _attributes;
    private int _attributeIndex = -1;

    public AttributeDataXPathNavigator(AttributeDataForest forest)
    {
        _forest = forest;
        _nameTable = new NameTable();
    }

    private AttributeDataXPathNavigator(AttributeDataXPathNavigator other)
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
    public AttributeDataForest Forest => _forest;

    /// <summary>
    /// The element the navigator is positioned on, or the element that owns the attribute it is positioned on.
    /// </summary>
    public AttributeDataElement? Element => _element;

    /// <summary>
    /// The name of the attribute the navigator is positioned on, if any.
    /// </summary>
    public string? AttributeName => IsOnAttribute ? _attributes![_attributeIndex].Name : null;

    /// <summary>
    /// The span of the attribute the element belongs to, when it is applied in the file that is analyzed.
    /// </summary>
    public ImmutableArray<TextSpan> ReportSpans => _element is null ? ImmutableArray<TextSpan>.Empty : _forest.GetSpans(_element);

    /// <summary>
    /// The name of the element, followed by the name of the attribute when the navigator is positioned on one, such
    /// as <c>AttributeData/@AttributeClassName</c>.
    /// </summary>
    public string? ReportName
    {
        get
        {
            if (_element is null)
                return null;

            return AttributeName is { } attributeName ? _element.Name + "/@" + attributeName : _element.Name;
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

            return _element is null ? "" : _element.Name;
        }
    }

    public override string Name => LocalName;

    // The elements and the attributes are not in a namespace
    public override string NamespaceURI => "";

    public override string Prefix => "";

    public override string BaseURI => "";

    public override bool IsEmptyElement => _element is not null && !IsOnAttribute && _element.Children.Length is 0;

    public override string Value
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Value;

            return _element is null ? "" : AttributeDataForest.GetValue(_element);
        }
    }

    public override XPathNavigator Clone() => new AttributeDataXPathNavigator(this);

    /// <summary>
    /// A navigator on the same forest, positioned on an element of it. It is how the <c>attributes</c> function
    /// returns the attributes of a symbol.
    /// </summary>
    public AttributeDataXPathNavigator CreateAt(AttributeDataElement element)
    {
        var navigator = new AttributeDataXPathNavigator(this);
        navigator.SetElement(element);
        return navigator;
    }

    public override bool IsSamePosition(XPathNavigator other)
    {
        return other is AttributeDataXPathNavigator navigator
            && ReferenceEquals(navigator._forest, _forest)
            && ReferenceEquals(navigator._element, _element)
            && navigator._attributeIndex == _attributeIndex;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        if (other is not AttributeDataXPathNavigator navigator || !ReferenceEquals(navigator._forest, _forest))
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

        // The attributes are the children of the document, so they are siblings of each other
        var siblings = _element.Parent is { } parent ? parent.Children : _forest.Roots;
        var index = _element.IndexInParent + step;
        if (index < 0 || index >= siblings.Length)
            return false;

        SetElement(siblings[index]);
        return true;
    }

    private void SetElement(AttributeDataElement element)
    {
        _element = element;
        _attributes = null;
        _attributeIndex = -1;
    }
}
