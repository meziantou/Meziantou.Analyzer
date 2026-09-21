using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes the operations of a file as an XML document, so they can be queried with XPath. Each operation is an
/// element of the <c>operation</c> namespace named after its <see cref="OperationKind"/>, such as
/// <c>operation:Invocation</c>, and the children of an element are the child operations. The properties of the
/// operation are the attributes of its element, named after the property, such as <c>IsImplicit</c> or
/// <c>TargetMethod</c>. The operations of a file form a forest, so the document has one element per root.
/// </summary>
internal sealed class OperationXPathNavigator : XPathNavigator, IBannedSyntaxNavigator
{
    private static readonly ConcurrentDictionary<Type, OperationProperty[]> OperationProperties = new();

    // The names of the kinds, which must be initialized before the set of the names
    private static readonly Dictionary<OperationKind, string> KindNames = CreateKindNames();
    private static readonly Dictionary<OperationKind, string> ElementNames = CreateElementNames();
    private static readonly HashSet<string> KindNameSet = new(KindNames.Values, StringComparer.Ordinal);

    // The members that are not attributes: the kind is the name of the element, the children are the child elements,
    // and the other ones are about the tree itself
    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        nameof(IOperation.Kind),
        nameof(IOperation.Syntax),
        nameof(IOperation.Parent),
        nameof(IOperation.SemanticModel),
        nameof(IOperation.Language),
        nameof(IOperation.ChildOperations),
        "Children",
    };

    private readonly OperationForest _forest;
    private readonly XmlNameTable _nameTable;
    private readonly CancellationToken _cancellationToken;

    // null when the navigator is positioned on the document, which is the parent of the roots
    private IOperation? _operation;

    // The index of _operation in the children of its parent, which allows moving to the siblings without searching the
    // operation. It is -1 when it is not known yet, as moving to the parent does not require it.
    private int _indexInParent;
    private XPathAttribute[]? _attributes;
    private int _attributeIndex = -1;

    public OperationXPathNavigator(OperationForest forest, CancellationToken cancellationToken)
    {
        _forest = forest;
        _nameTable = new NameTable();
        _cancellationToken = cancellationToken;
    }

    private OperationXPathNavigator(OperationXPathNavigator other)
    {
        _forest = other._forest;
        _nameTable = other._nameTable;
        _cancellationToken = other._cancellationToken;
        _operation = other._operation;
        _indexInParent = other._indexInParent;
        _attributes = other._attributes;
        _attributeIndex = other._attributeIndex;
    }

    /// <summary>
    /// The operation the navigator is positioned on, or the operation that owns the attribute it is positioned on.
    /// </summary>
    public IOperation? Operation => _operation;

    /// <summary>
    /// The name of the attribute the navigator is positioned on, if any.
    /// </summary>
    public string? AttributeName => IsOnAttribute ? _attributes![_attributeIndex].Name : null;

    /// <summary>
    /// The span of the syntax of the operation the navigator is positioned on.
    /// </summary>
    public TextSpan Span => IsOnAttribute ? _attributes![_attributeIndex].Span : _operation?.Syntax.Span ?? default;

    /// <summary>
    /// The qualified name of the element, followed by the name of the attribute when the navigator is positioned on
    /// one, such as <c>operation:Invocation/@TargetMethod</c>.
    /// </summary>
    public string? ReportName
    {
        get
        {
            if (_operation is null)
                return null;

            var name = GetElementName(_operation.Kind);
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

            return _operation is null ? XPathNodeType.Root : XPathNodeType.Element;
        }
    }

    public override string LocalName
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].LocalName;

            return _operation is null ? "" : GetKindName(_operation.Kind);
        }
    }

    public override string Name
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Name;

            return _operation is null ? "" : GetElementName(_operation.Kind);
        }
    }

    // The attributes are not in a namespace, so they are never prefixed
    public override string NamespaceURI => !IsOnAttribute && _operation is not null ? XPathNamespaces.OperationNamespaceUri : "";

    public override string Prefix => !IsOnAttribute && _operation is not null ? XPathNamespaces.OperationPrefix : "";

    public override string BaseURI => "";

    public override bool IsEmptyElement => _operation is not null && !IsOnAttribute && _forest.GetChildren(_operation).Length is 0;

    public override string Value
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].Value;

            return _operation is null ? "" : _operation.Syntax.ToString();
        }
    }

    public override XPathNavigator Clone() => new OperationXPathNavigator(this);

    public override bool IsSamePosition(XPathNavigator other)
    {
        return other is OperationXPathNavigator navigator
            && ReferenceEquals(navigator._forest, _forest)
            && ReferenceEquals(navigator._operation, _operation)
            && navigator._attributeIndex == _attributeIndex;
    }

    public override bool MoveTo(XPathNavigator other)
    {
        if (other is not OperationXPathNavigator navigator || !ReferenceEquals(navigator._forest, _forest))
            return false;

        _operation = navigator._operation;
        _indexInParent = navigator._indexInParent;
        _attributes = navigator._attributes;
        _attributeIndex = navigator._attributeIndex;
        return true;
    }

    public override bool MoveToFirstAttribute()
    {
        if (_operation is null || IsOnAttribute)
            return false;

        _attributes ??= _forest.GetAttributes(_operation);
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

        var children = _operation is null ? _forest.Roots : _forest.GetChildren(_operation);
        if (children.Length is 0)
            return false;

        SetOperation(children[0], indexInParent: 0);
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

        if (_operation is null)
            return false;

        if (_operation.Parent is not { } parent)
        {
            MoveToRoot();
            return true;
        }

        SetOperation(parent, indexInParent: -1);
        return true;
    }

    public override void MoveToRoot()
    {
        _operation = null;
        _indexInParent = 0;
        _attributes = null;
        _attributeIndex = -1;
    }

    private bool MoveToSibling(int step)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        if (IsOnAttribute || _operation is null)
            return false;

        var siblings = GetSiblings(_operation);
        if (_indexInParent < 0)
        {
            _indexInParent = Array.IndexOf(siblings, _operation);
        }

        var index = _indexInParent + step;
        if (index < 0 || index >= siblings.Length)
            return false;

        SetOperation(siblings[index], index);
        return true;
    }

    // The roots are the children of the document, so they are siblings of each other
    private IOperation[] GetSiblings(IOperation operation) => operation.Parent is { } parent ? _forest.GetChildren(parent) : _forest.Roots;

    private void SetOperation(IOperation operation, int indexInParent)
    {
        _operation = operation;
        _indexInParent = indexInParent;
        _attributes = null;
        _attributeIndex = -1;
    }

    /// <summary>
    /// Indicates whether a name is the one of a kind of operation, as the elements are named.
    /// </summary>
    public static bool IsKindName(string name) => KindNameSet.Contains(name);

    private static string GetKindName(OperationKind kind) => KindNames.TryGetValue(kind, out var name) ? name : kind.ToString();

    private static string GetElementName(OperationKind kind) => ElementNames.TryGetValue(kind, out var name) ? name : XPathNamespaces.OperationPrefix + ":" + GetKindName(kind);

    // Several members of OperationKind share a value, such as 'Binary' and 'BinaryOperator', and the name ToString
    // returns for such a value is not the same in every version of Roslyn. The shortest name is the current one, and
    // it does not depend on the version, so the elements are named after it.
    private static Dictionary<OperationKind, string> CreateKindNames()
    {
        var names = new Dictionary<OperationKind, string>();
        foreach (var field in typeof(OperationKind).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OperationKind kind)
                continue;

            var name = field.Name;
            if (!names.TryGetValue(kind, out var existing) || name.Length < existing.Length || (name.Length == existing.Length && string.CompareOrdinal(name, existing) < 0))
            {
                names[kind] = name;
            }
        }

        return names;
    }

    private static Dictionary<OperationKind, string> CreateElementNames()
    {
        var names = new Dictionary<OperationKind, string>(KindNames.Count);
        foreach (var pair in KindNames)
        {
            names.Add(pair.Key, XPathNamespaces.OperationPrefix + ":" + pair.Value);
        }

        return names;
    }

    internal static XPathAttribute[] BuildAttributes(IOperation operation)
    {
        var attributes = new List<XPathAttribute>();
        var writer = new XPathAttributeWriter(attributes, namespaceUri: "", qualifiedNames: null);
        var span = operation.Syntax.Span;
        foreach (var property in GetProperties(operation))
        {
            object? value;
            try
            {
                value = property.Property.GetValue(operation);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            if (value is null)
                continue;

            switch (property.Kind)
            {
                case OperationPropertyKind.Type:
                    XPathAttributeFormatter.AddType(writer, span, (ITypeSymbol)value, property.TypeNames!);
                    break;

                case OperationPropertyKind.Symbol:
                    XPathAttributeFormatter.AddSymbol(writer, span, (ISymbol)value, property.SymbolNames!);
                    break;

                case OperationPropertyKind.Symbols:
                    AddSymbols(writer, span, value, property.SymbolNames!);
                    break;

                case OperationPropertyKind.Boolean:
                    writer.Add(span, property.Name, XPathAttributeFormatter.ToXPathBoolean((bool)value));
                    break;

                case OperationPropertyKind.Enumeration:
                    writer.Add(span, property.Name, value.ToString());
                    break;

                case OperationPropertyKind.String:
                    writer.Add(span, property.Name, (string)value);
                    break;

                case OperationPropertyKind.Int32:
                    writer.Add(span, property.Name, ((int)value).ToString(CultureInfo.InvariantCulture));
                    break;

                case OperationPropertyKind.Constant:
                    var constantValue = (Optional<object>)value;
                    if (constantValue.HasValue)
                    {
                        writer.Add(span, property.HasValueName!, "true");
                        writer.Add(span, property.Name, XPathAttributeFormatter.FormatConstantValue(constantValue.Value));
                    }

                    break;
            }
        }

        return attributes.Count is 0 ? [] : [.. attributes];
    }

    // The symbols of a property that returns several of them are joined by a space, like the tokens of a SyntaxTokenList
    private static void AddSymbols(in XPathAttributeWriter writer, TextSpan span, object value, SymbolAttributeNames names)
    {
        var qualifiedNames = new List<string>();
        var shortNames = new List<string>();
        try
        {
            foreach (var item in (System.Collections.IEnumerable)value)
            {
                if (item is not ISymbol symbol)
                    continue;

                if (SymbolNameFormatter.GetSymbolName(symbol) is { Length: > 0 } qualifiedName)
                {
                    qualifiedNames.Add(qualifiedName);
                }

                if (symbol.Name is { Length: > 0 } name)
                {
                    shortNames.Add(name);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // A default ImmutableArray cannot be enumerated
            return;
        }

        writer.Add(span, names.QualifiedName, string.Join(" ", qualifiedNames));
        writer.Add(span, names.Name, string.Join(" ", shortNames));
    }

    private static OperationProperty[] GetProperties(IOperation operation)
    {
        return OperationProperties.GetOrAdd(operation.GetType(), static type =>
        {
            // The operations are implemented by internal types, so the properties are the ones of their interfaces.
            // The interfaces overlap, and a few of them redeclare a property, so they are deduplicated by name.
            var properties = new Dictionary<string, OperationProperty>(StringComparer.Ordinal);
            foreach (var contract in type.GetInterfaces())
            {
                if (!typeof(IOperation).IsAssignableFrom(contract))
                    continue;

                if (contract.Namespace is not ("Microsoft.CodeAnalysis" or "Microsoft.CodeAnalysis.Operations"))
                    continue;

                foreach (var property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.GetIndexParameters().Length is not 0)
                        continue;

                    if (property.GetCustomAttribute<ObsoleteAttribute>() is not null)
                        continue;

                    if (IgnoredProperties.Contains(property.Name) || properties.ContainsKey(property.Name))
                        continue;

                    if (CreateProperty(property) is { } operationProperty)
                    {
                        properties.Add(property.Name, operationProperty);
                    }
                }
            }

            return [.. properties.Values.OrderBy(property => property.Name, StringComparer.Ordinal)];
        });
    }

    // The names of the attributes are computed once, so nothing is concatenated when an operation is visited
    private static OperationProperty? CreateProperty(PropertyInfo property)
    {
        var name = property.Name;
        var type = property.PropertyType;

        if (typeof(ITypeSymbol).IsAssignableFrom(type))
            return new OperationProperty(property, OperationPropertyKind.Type, name, CreateTypeNames(name), SymbolNames: null, HasValueName: null);

        if (typeof(ISymbol).IsAssignableFrom(type))
            return new OperationProperty(property, OperationPropertyKind.Symbol, name, TypeNames: null, CreateSymbolNames(name), HasValueName: null);

        if (type == typeof(Optional<object>))
            return new OperationProperty(property, OperationPropertyKind.Constant, name, TypeNames: null, SymbolNames: null, HasValueName: "Has" + name);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>) && typeof(ISymbol).IsAssignableFrom(type.GetGenericArguments()[0]))
            return new OperationProperty(property, OperationPropertyKind.Symbols, name, TypeNames: null, CreateSymbolNames(name), HasValueName: null);

        if (type == typeof(bool))
            return new OperationProperty(property, OperationPropertyKind.Boolean, name, TypeNames: null, SymbolNames: null, HasValueName: null);

        if (type == typeof(string))
            return new OperationProperty(property, OperationPropertyKind.String, name, TypeNames: null, SymbolNames: null, HasValueName: null);

        if (type == typeof(int))
            return new OperationProperty(property, OperationPropertyKind.Int32, name, TypeNames: null, SymbolNames: null, HasValueName: null);

        if (type.IsEnum)
            return new OperationProperty(property, OperationPropertyKind.Enumeration, name, TypeNames: null, SymbolNames: null, HasValueName: null);

        // The other types, such as IOperation or CommonConversion, are the child elements or are not exposed
        return null;
    }

    private static TypeAttributeNames CreateTypeNames(string name)
        => new(name + "Name", name + "MetadataName", name + "DocumentationId", name + "ReferenceId", name + "IsValueType", name + "NullableAnnotation", name + "SpecialType");

    private static SymbolAttributeNames CreateSymbolNames(string name)
        => new(name, name + "Name", name + "DocumentationId", name + "Kind", name + "IsStatic");

    private enum OperationPropertyKind
    {
        Type,
        Symbol,
        Symbols,
        Boolean,
        Enumeration,
        String,
        Int32,
        Constant,
    }

    private sealed record OperationProperty(PropertyInfo Property, OperationPropertyKind Kind, string Name, TypeAttributeNames? TypeNames, SymbolAttributeNames? SymbolNames, string? HasValueName);
}
