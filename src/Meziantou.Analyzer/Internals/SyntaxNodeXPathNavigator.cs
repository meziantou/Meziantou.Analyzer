using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Exposes a syntax tree as an XML document, so it can be queried with XPath. Each syntax node is an element named
/// after its <see cref="SyntaxKind"/>, and the tokens of a node (<c>Identifier</c>, <c>Modifiers</c>, <c>Keyword</c>, ...)
/// are the attributes of its element, named after the property of the node that returns them.
/// When a <see cref="SemanticModel"/> is provided, the data of the semantic model is exposed as additional attributes
/// in the <c>semantic</c> namespace (<c>semantic:Type</c>, <c>semantic:Symbol</c>, ...).
/// </summary>
internal sealed class SyntaxNodeXPathNavigator : XPathNavigator, IBannedSyntaxNavigator
{
    /// <summary>The prefix of the attributes that expose the data of the semantic model.</summary>
    public const string SemanticPrefix = XPathNamespaces.SemanticPrefix;

    private const string SemanticNamespaceUri = XPathNamespaces.SemanticNamespaceUri;

    // The attributes that expose a type, grouped by the type they expose. They must be initialized before the
    // dictionary of the names, as a static field initializer runs in the order of the declarations.
    private static readonly TypeAttributeNames TypeNames = new(SemanticName.TypeName, SemanticName.TypeMetadataName, SemanticName.TypeDocumentationId, SemanticName.TypeReferenceId, SemanticName.TypeKind, SemanticName.TypeIsValueType, SemanticName.TypeNullableAnnotation, SemanticName.TypeSpecialType);
    private static readonly TypeAttributeNames ConvertedTypeNames = new(SemanticName.ConvertedTypeName, SemanticName.ConvertedTypeMetadataName, SemanticName.ConvertedTypeDocumentationId, SemanticName.ConvertedTypeReferenceId, SemanticName.ConvertedTypeKind, SemanticName.ConvertedTypeIsValueType, SemanticName.ConvertedTypeNullableAnnotation, SemanticName.ConvertedTypeSpecialType);
    private static readonly TypeAttributeNames ReturnTypeNames = new(SemanticName.ReturnTypeName, SemanticName.ReturnTypeMetadataName, SemanticName.ReturnTypeDocumentationId, SemanticName.ReturnTypeReferenceId, SemanticName.ReturnTypeKind, SemanticName.ReturnTypeIsValueType, SemanticName.ReturnTypeNullableAnnotation, SemanticName.ReturnTypeSpecialType);
    private static readonly TypeAttributeNames ContainingTypeNames = new(SemanticName.ContainingTypeName, SemanticName.ContainingTypeMetadataName, SemanticName.ContainingTypeDocumentationId, SemanticName.ContainingTypeReferenceId, SemanticName.ContainingTypeKind, SemanticName.ContainingTypeIsValueType, SemanticName.ContainingTypeNullableAnnotation, SemanticName.ContainingTypeSpecialType);

    // The attributes that expose the symbol of a node. The accessibility is not one of them, as it is the only one
    // that is not shared with the symbols the operations expose.
    private static readonly SymbolAttributeNames SemanticSymbolNames = new(
        SemanticName.Symbol,
        SemanticName.SymbolName,
        SemanticName.SymbolDocumentationId,
        SemanticName.SymbolKind,
        SemanticName.IsStatic,
        SemanticName.IsAbstract,
        SemanticName.IsVirtual,
        SemanticName.IsOverride,
        SemanticName.IsSealed,
        SemanticName.IsAsync,
        SemanticName.IsExtensionMethod,
        SemanticName.Arity);

    // The local names of the attributes of the 'semantic' namespace, mapped to their qualified name
    private static readonly Dictionary<string, string> SemanticNames = CreateSemanticNames(
    [
        .. TypeNames.All,
        .. ConvertedTypeNames.All,
        .. ReturnTypeNames.All,
        .. ContainingTypeNames.All,
        .. SemanticSymbolNames.All,
        SemanticName.ContainingSymbol,
        SemanticName.ContainingSymbolName,
        SemanticName.ContainingSymbolDocumentationId,
        SemanticName.ContainingSymbolKind,
        SemanticName.DeclaredAccessibility,
        SemanticName.HasConstantValue,
        SemanticName.ConstantValue,
    ]);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> TokenProperties = new();
    private static readonly ConcurrentDictionary<SyntaxKind, string> KindNames = new();

    private readonly SyntaxForest _forest;
    private readonly SyntaxNode _root;
    private readonly XmlNameTable _nameTable;
    private readonly CancellationToken _cancellationToken;

    // null when the navigator is positioned on the document, which is the parent of the root node
    private SyntaxNode? _node;

    // The index of _node in the ChildNodesAndTokens of its parent, which allows moving to the siblings without searching the node.
    // It is -1 when it is not known yet, as moving to the parent does not require it.
    private int _indexInParent;
    private XPathAttribute[]? _attributes;
    private int _attributeIndex = -1;

    public SyntaxNodeXPathNavigator(SyntaxForest forest)
    {
        _forest = forest;
        _root = forest.Root;
        _nameTable = new NameTable();
        _cancellationToken = forest.CancellationToken;
    }

    private SyntaxNodeXPathNavigator(SyntaxNodeXPathNavigator other)
    {
        _forest = other._forest;
        _root = other._root;
        _nameTable = other._nameTable;
        _cancellationToken = other._cancellationToken;
        _node = other._node;
        _indexInParent = other._indexInParent;
        _attributes = other._attributes;
        _attributeIndex = other._attributeIndex;
    }

    /// <summary>
    /// Indicates whether a name is one of the attributes of the <c>semantic</c> namespace.
    /// </summary>
    public static bool IsSemanticName(string name) => SemanticNames.ContainsKey(name);

    /// <summary>
    /// The node the navigator is positioned on, or the node that owns the attribute the navigator is positioned on.
    /// </summary>
    public SyntaxNode? Node => _node;

    /// <summary>
    /// The name of the attribute the navigator is positioned on, if any. It is prefixed for the semantic attributes.
    /// </summary>
    public string? AttributeName => IsOnAttribute ? _attributes![_attributeIndex].Name : null;

    /// <summary>
    /// The span of the node or of the tokens of the attribute the navigator is positioned on.
    /// </summary>
    public TextSpan Span => IsOnAttribute ? _attributes![_attributeIndex].Span : _node?.Span ?? _root.Span;

    /// <summary>
    /// The kind of the node, followed by the name of the attribute when the navigator is positioned on one.
    /// </summary>
    public string? ReportName
    {
        get
        {
            if (_node is null)
                return null;

            var name = GetKindName(_node.Kind());
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

            return _node is null ? XPathNodeType.Root : XPathNodeType.Element;
        }
    }

    public override string LocalName
    {
        get
        {
            if (IsOnAttribute)
                return _attributes![_attributeIndex].LocalName;

            return _node is null ? "" : GetKindName(_node.Kind());
        }
    }

    public override string Name => IsOnAttribute ? _attributes![_attributeIndex].Name : LocalName;

    public override string NamespaceURI => IsOnAttribute ? _attributes![_attributeIndex].NamespaceUri : "";

    public override string Prefix => IsOnAttribute && _attributes![_attributeIndex].NamespaceUri.Length > 0 ? SemanticPrefix : "";

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

    /// <summary>
    /// A navigator on the same tree, positioned on a node of it. It is how the <c>syntax</c> function returns the
    /// node of an operation.
    /// </summary>
    public SyntaxNodeXPathNavigator CreateAt(SyntaxNode node)
    {
        var navigator = new SyntaxNodeXPathNavigator(this);
        navigator.MoveToNode(node);
        return navigator;
    }

    /// <summary>
    /// Moves the navigator to a node of the tree. The index in the parent is not known, so it is computed when the
    /// navigator moves to a sibling.
    /// </summary>
    public void MoveToNode(SyntaxNode node) => SetNode(node, node == _root ? 0 : -1);

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

        _attributes ??= _forest.GetAttributes(_node);
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

        var children = _node.ChildNodesAndTokens();
        var index = FindChildNode(children, startIndex: 0, step: 1);
        if (index < 0)
            return false;

        SetNode(children[index].AsNode()!, index);
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

    private static Dictionary<string, string> CreateSemanticNames(string[] names)
    {
        var result = new Dictionary<string, string>(names.Length, StringComparer.Ordinal);
        foreach (var name in names)
        {
            result.Add(name, SemanticPrefix + ":" + name);
        }

        return result;
    }

    /// <summary>
    /// The attributes of a node. It is <see cref="SyntaxForest.GetAttributes(SyntaxNode)"/> that calls it, so they
    /// are computed once per file.
    /// </summary>
    internal static XPathAttribute[] BuildAttributes(SyntaxNode node, SyntaxForest forest)
    {
        var attributes = new List<XPathAttribute>();
        AddTokenAttributes(attributes, node, forest.Selection.Filter);
        AddSemanticAttributes(attributes, node, forest);
        return attributes.Count is 0 ? [] : [.. attributes];
    }

    private static void AddTokenAttributes(List<XPathAttribute> attributes, SyntaxNode node, XPathAttributeFilter filter)
    {
        var properties = TokenProperties.GetOrAdd(node.GetType(), static type =>
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length is 0)
                .Where(property => property.PropertyType == typeof(SyntaxToken) || property.PropertyType == typeof(SyntaxTokenList))
                .Where(property => property.GetCustomAttribute<ObsoleteAttribute>() is null)
                .OrderBy(property => property.Name, StringComparer.Ordinal),
        ]);

        foreach (var property in properties)
        {
            // Reading the property is a reflection call, so it is only done when a query can select the attribute
            if (!filter.Includes(property.Name))
                continue;

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
                    attributes.Add(new XPathAttribute(property.Name, property.Name, NamespaceUri: "", token.Text, token.Span));
                    break;

                case SyntaxTokenList { Count: > 0 } tokens:
                    attributes.Add(new XPathAttribute(property.Name, property.Name, NamespaceUri: "", JoinTokens(tokens), tokens.Span));
                    break;
            }
        }
    }

    // The tokens of a list are joined by a space, so a query can test one of them with the 'contains' function
    private static string JoinTokens(SyntaxTokenList tokens)
    {
        if (tokens.Count is 1)
            return tokens[0].Text;

        var builder = ObjectPool.SharedStringBuilderPool.Get();
        try
        {
            foreach (var token in tokens)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(token.Text);
            }

            return builder.ToString();
        }
        finally
        {
            ObjectPool.SharedStringBuilderPool.Return(builder);
        }
    }

    private static void AddSemanticAttributes(List<XPathAttribute> attributes, SyntaxNode node, SyntaxForest forest)
    {
        var semanticModel = forest.SemanticModel;
        if (semanticModel is null)
            return;

        var selection = forest.Selection;
        if (!selection.IncludesAny)
            return;

        var cancellationToken = forest.CancellationToken;
        var writer = new XPathAttributeWriter(attributes, SemanticNamespaceUri, SemanticNames, selection.Filter);
        var span = node.Span;

        // Asking the semantic model is the expensive part, so it is only asked for the groups a query can select
        if (selection.IncludesTypeInfo)
        {
            var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
            XPathAttributeFormatter.AddType(writer, span, typeInfo.Type, TypeNames);
            XPathAttributeFormatter.AddType(writer, span, typeInfo.ConvertedType, ConvertedTypeNames);
        }

        if (selection.IncludesSymbol)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault() ?? semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (symbol is not null)
            {
                if (writer.Includes(SemanticName.Symbol))
                {
                    writer.Add(span, SemanticName.Symbol, SymbolNameFormatter.GetSymbolName(symbol));
                }

                writer.Add(span, SemanticName.SymbolName, symbol.Name);

                if (writer.Includes(SemanticName.SymbolDocumentationId))
                {
                    writer.Add(span, SemanticName.SymbolDocumentationId, SymbolNameFormatter.GetDocumentationId(symbol));
                }

                writer.Add(span, SemanticName.SymbolKind, XPathAttributeFormatter.GetSymbolKindName(symbol.Kind));
                writer.Add(span, SemanticName.DeclaredAccessibility, XPathAttributeFormatter.GetAccessibilityName(symbol.DeclaredAccessibility));
                XPathAttributeFormatter.AddSymbolModifiers(writer, span, symbol, SemanticSymbolNames);

                if (selection.IncludesContainingType)
                {
                    XPathAttributeFormatter.AddType(writer, span, symbol.ContainingType, ContainingTypeNames);
                }

                // The containing symbol is the containing type for a member, but it is the method for a local or a
                // parameter, and the namespace for a type
                if (selection.IncludesContainingSymbol && symbol.ContainingSymbol is { } containingSymbol)
                {
                    if (writer.Includes(SemanticName.ContainingSymbol))
                    {
                        writer.Add(span, SemanticName.ContainingSymbol, SymbolNameFormatter.GetSymbolName(containingSymbol));
                    }

                    writer.Add(span, SemanticName.ContainingSymbolName, containingSymbol.Name);

                    if (writer.Includes(SemanticName.ContainingSymbolDocumentationId))
                    {
                        writer.Add(span, SemanticName.ContainingSymbolDocumentationId, SymbolNameFormatter.GetDocumentationId(containingSymbol));
                    }

                    writer.Add(span, SemanticName.ContainingSymbolKind, XPathAttributeFormatter.GetSymbolKindName(containingSymbol.Kind));
                }

                if (selection.IncludesReturnType && symbol is IMethodSymbol method)
                {
                    XPathAttributeFormatter.AddType(writer, span, method.ReturnType, ReturnTypeNames);
                }
            }
        }

        if (selection.IncludesConstantValue && node is ExpressionSyntax)
        {
            var constantValue = semanticModel.GetConstantValue(node, cancellationToken);
            if (constantValue.HasValue)
            {
                writer.Add(span, SemanticName.HasConstantValue, "true");
                writer.Add(span, SemanticName.ConstantValue, XPathAttributeFormatter.FormatConstantValue(constantValue.Value));
            }
        }
    }

    /// <summary>
    /// The groups of semantic attributes the queries of a file can select. The groups are computed once per file, as
    /// a group is a single call to the semantic model that produces several attributes.
    /// </summary>
    internal sealed class AttributeSelection
    {
        // The attributes that come from the same call to the semantic model
        private static readonly string[] TypeInfoNames = [.. TypeNames.All, .. ConvertedTypeNames.All];
        private static readonly string[] SymbolNames =
        [
            .. SemanticSymbolNames.All,
            SemanticName.DeclaredAccessibility,
            .. ContainingTypeNames.All,
            SemanticName.ContainingSymbol,
            SemanticName.ContainingSymbolName,
            SemanticName.ContainingSymbolDocumentationId,
            SemanticName.ContainingSymbolKind,
            .. ReturnTypeNames.All,
        ];

        private static readonly string[] ContainingSymbolNames =
        [
            SemanticName.ContainingSymbol,
            SemanticName.ContainingSymbolName,
            SemanticName.ContainingSymbolDocumentationId,
            SemanticName.ContainingSymbolKind,
        ];

        private static readonly string[] ConstantValueNames = [SemanticName.HasConstantValue, SemanticName.ConstantValue];

        public AttributeSelection(XPathAttributeFilter filter)
        {
            Filter = filter;
            IncludesTypeInfo = filter.IncludesAny(TypeInfoNames);
            IncludesSymbol = filter.IncludesAny(SymbolNames);
            IncludesContainingType = filter.IncludesAny(ContainingTypeNames.All);
            IncludesContainingSymbol = filter.IncludesAny(ContainingSymbolNames);
            IncludesReturnType = filter.IncludesAny(ReturnTypeNames.All);
            IncludesConstantValue = filter.IncludesAny(ConstantValueNames);
        }

        public XPathAttributeFilter Filter { get; }

        /// <summary>Indicates whether the semantic model is asked for at least one group of attributes.</summary>
        public bool IncludesAny => IncludesTypeInfo || IncludesSymbol || IncludesConstantValue;

        public bool IncludesTypeInfo { get; }

        public bool IncludesSymbol { get; }

        public bool IncludesContainingType { get; }

        public bool IncludesContainingSymbol { get; }

        public bool IncludesReturnType { get; }

        public bool IncludesConstantValue { get; }
    }

    private static class SemanticName
    {
        public const string TypeName = nameof(TypeName);
        public const string TypeMetadataName = nameof(TypeMetadataName);
        public const string TypeDocumentationId = nameof(TypeDocumentationId);
        public const string TypeReferenceId = nameof(TypeReferenceId);
        public const string TypeKind = nameof(TypeKind);
        public const string TypeIsValueType = nameof(TypeIsValueType);
        public const string TypeNullableAnnotation = nameof(TypeNullableAnnotation);
        public const string TypeSpecialType = nameof(TypeSpecialType);
        public const string ConvertedTypeName = nameof(ConvertedTypeName);
        public const string ConvertedTypeMetadataName = nameof(ConvertedTypeMetadataName);
        public const string ConvertedTypeDocumentationId = nameof(ConvertedTypeDocumentationId);
        public const string ConvertedTypeReferenceId = nameof(ConvertedTypeReferenceId);
        public const string ConvertedTypeKind = nameof(ConvertedTypeKind);
        public const string ConvertedTypeIsValueType = nameof(ConvertedTypeIsValueType);
        public const string ConvertedTypeNullableAnnotation = nameof(ConvertedTypeNullableAnnotation);
        public const string ConvertedTypeSpecialType = nameof(ConvertedTypeSpecialType);
        public const string ReturnTypeName = nameof(ReturnTypeName);
        public const string ReturnTypeMetadataName = nameof(ReturnTypeMetadataName);
        public const string ReturnTypeDocumentationId = nameof(ReturnTypeDocumentationId);
        public const string ReturnTypeReferenceId = nameof(ReturnTypeReferenceId);
        public const string ReturnTypeKind = nameof(ReturnTypeKind);
        public const string ReturnTypeIsValueType = nameof(ReturnTypeIsValueType);
        public const string ReturnTypeNullableAnnotation = nameof(ReturnTypeNullableAnnotation);
        public const string ReturnTypeSpecialType = nameof(ReturnTypeSpecialType);
        public const string ContainingTypeName = nameof(ContainingTypeName);
        public const string ContainingTypeMetadataName = nameof(ContainingTypeMetadataName);
        public const string ContainingTypeDocumentationId = nameof(ContainingTypeDocumentationId);
        public const string ContainingTypeReferenceId = nameof(ContainingTypeReferenceId);
        public const string ContainingTypeKind = nameof(ContainingTypeKind);
        public const string ContainingTypeIsValueType = nameof(ContainingTypeIsValueType);
        public const string ContainingTypeNullableAnnotation = nameof(ContainingTypeNullableAnnotation);
        public const string ContainingTypeSpecialType = nameof(ContainingTypeSpecialType);
        public const string Symbol = nameof(Symbol);
        public const string SymbolName = nameof(SymbolName);
        public const string SymbolDocumentationId = nameof(SymbolDocumentationId);
        public const string SymbolKind = nameof(SymbolKind);
        public const string ContainingSymbol = nameof(ContainingSymbol);
        public const string ContainingSymbolName = nameof(ContainingSymbolName);
        public const string ContainingSymbolDocumentationId = nameof(ContainingSymbolDocumentationId);
        public const string ContainingSymbolKind = nameof(ContainingSymbolKind);
        public const string DeclaredAccessibility = nameof(DeclaredAccessibility);
        public const string IsStatic = nameof(IsStatic);
        public const string IsAbstract = nameof(IsAbstract);
        public const string IsVirtual = nameof(IsVirtual);
        public const string IsOverride = nameof(IsOverride);
        public const string IsSealed = nameof(IsSealed);
        public const string IsAsync = nameof(IsAsync);
        public const string IsExtensionMethod = nameof(IsExtensionMethod);
        public const string Arity = nameof(Arity);
        public const string HasConstantValue = nameof(HasConstantValue);
        public const string ConstantValue = nameof(ConstantValue);
    }
}
