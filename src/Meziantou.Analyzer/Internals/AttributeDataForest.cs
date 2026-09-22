using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The attributes applied to a symbol, as the <c>attributes</c> function returns them. Each attribute is an element,
/// whose children are its constructor arguments followed by its named arguments. The items of an argument that is an
/// array are the children of the argument. It is built once per symbol and per file, and shared by the navigator and
/// all its clones.
/// </summary>
internal sealed class AttributeDataForest
{
    private static readonly TypeAttributeNames AttributeClassNames = XPathPropertyAttributes.CreateTypeNames("AttributeClass");
    private static readonly SymbolAttributeNames AttributeConstructorNames = XPathPropertyAttributes.CreateSymbolNames("AttributeConstructor");
    private static readonly TypeAttributeNames TypeNames = XPathPropertyAttributes.CreateTypeNames("Type");
    private static readonly TypeAttributeNames ValueTypeNames = XPathPropertyAttributes.CreateTypeNames("Value");

    private readonly SyntaxTree? _syntaxTree;
    private readonly CancellationToken _cancellationToken;

    private AttributeDataForest(AttributeDataElement[] roots, SyntaxTree? syntaxTree, CancellationToken cancellationToken)
    {
        Roots = roots;
        _syntaxTree = syntaxTree;
        _cancellationToken = cancellationToken;
    }

    /// <summary>The attributes applied to the symbol, in the order Roslyn returns them.</summary>
    public AttributeDataElement[] Roots { get; }

    public CancellationToken CancellationToken => _cancellationToken;

    /// <param name="symbol">The symbol the attributes are applied to.</param>
    /// <param name="syntaxTree">The file that is analyzed. Only the attributes applied in this file are reportable.</param>
    /// <param name="cancellationToken">The cancellation token of the analysis.</param>
    public static AttributeDataForest Create(ISymbol symbol, SyntaxTree? syntaxTree, CancellationToken cancellationToken)
    {
        var attributes = symbol.GetAttributes();
        var roots = new AttributeDataElement[attributes.Length];
        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            var element = new AttributeDataElement(AttributeDataElement.AttributeDataName, attribute, constant: default, argumentName: null, position: -1) { IndexInParent = i };
            var children = new List<AttributeDataElement>(attribute.ConstructorArguments.Length + attribute.NamedArguments.Length);
            for (var position = 0; position < attribute.ConstructorArguments.Length; position++)
            {
                children.Add(CreateArgument(AttributeDataElement.ConstructorArgumentName, attribute, attribute.ConstructorArguments[position], argumentName: null, position));
            }

            foreach (var namedArgument in attribute.NamedArguments)
            {
                children.Add(CreateArgument(AttributeDataElement.NamedArgumentName, attribute, namedArgument.Value, namedArgument.Key, position: -1));
            }

            SetChildren(element, children);
            roots[i] = element;
        }

        return new AttributeDataForest(roots, syntaxTree, cancellationToken);
    }

    private static AttributeDataElement CreateArgument(string name, AttributeData attribute, TypedConstant constant, string? argumentName, int position)
    {
        var element = new AttributeDataElement(name, attribute, constant, argumentName, position);
        if (constant is { Kind: TypedConstantKind.Array, IsNull: false })
        {
            var items = new List<AttributeDataElement>(constant.Values.Length);
            for (var i = 0; i < constant.Values.Length; i++)
            {
                items.Add(CreateArgument(AttributeDataElement.ItemName, attribute, constant.Values[i], argumentName: null, i));
            }

            SetChildren(element, items);
        }

        return element;
    }

    private static void SetChildren(AttributeDataElement parent, List<AttributeDataElement> children)
    {
        for (var i = 0; i < children.Count; i++)
        {
            children[i].Parent = parent;
            children[i].IndexInParent = i;
        }

        parent.Children = [.. children];
    }

    /// <summary>
    /// The span of the attribute an element belongs to, when the attribute is applied in the file that is analyzed.
    /// An argument is reported on its attribute, as a <c>params</c> array has no syntax of its own.
    /// </summary>
    public ImmutableArray<TextSpan> GetSpans(AttributeDataElement element)
    {
        if (element.Attribute.ApplicationSyntaxReference is { } reference && reference.SyntaxTree == _syntaxTree)
            return ImmutableArray.Create(reference.Span);

        return ImmutableArray<TextSpan>.Empty;
    }

    /// <summary>
    /// The syntax of the attribute an element belongs to, when the attribute is applied in the file that is analyzed.
    /// </summary>
    public SyntaxNode? GetSyntaxNode(AttributeDataElement element)
    {
        if (element.Attribute.ApplicationSyntaxReference is { } reference && reference.SyntaxTree == _syntaxTree)
            return reference.GetSyntax(_cancellationToken);

        return null;
    }

    /// <summary>
    /// The value of an element: the attribute as Roslyn displays it, or the value of an argument.
    /// </summary>
    public static string GetValue(AttributeDataElement element) => element.IsAttribute ? element.Attribute.ToString() ?? "" : FormatValue(element.Constant) ?? "";

    /// <summary>
    /// The attributes of an element. The attributes of the elements are few, so all of them are computed, once.
    /// </summary>
    public XPathAttribute[] GetAttributes(AttributeDataElement element)
    {
        return element.Attributes ??= BuildAttributes(element, GetSpans(element));
    }

    private static XPathAttribute[] BuildAttributes(AttributeDataElement element, ImmutableArray<TextSpan> spans)
    {
        var attributes = new List<XPathAttribute>();
        var writer = new XPathAttributeWriter(attributes, namespaceUri: "", qualifiedNames: null, XPathAttributeFilter.All);
        var span = spans.IsDefaultOrEmpty ? default : spans[0];
        if (element.IsAttribute)
        {
            XPathAttributeFormatter.AddType(writer, span, element.Attribute.AttributeClass, AttributeClassNames);
            XPathAttributeFormatter.AddSymbol(writer, span, element.Attribute.AttributeConstructor, AttributeConstructorNames);
        }
        else
        {
            var constant = element.Constant;
            writer.Add(span, AttributeName.Name, element.ArgumentName);
            if (element.Position >= 0)
            {
                writer.Add(span, AttributeName.Position, element.Position.ToString(CultureInfo.InvariantCulture));
            }

            writer.Add(span, AttributeName.Kind, constant.Kind.ToString());
            writer.Add(span, AttributeName.IsNull, XPathAttributeFormatter.ToXPathBoolean(constant.IsNull));
            writer.Add(span, AttributeName.Value, FormatValue(constant));
            XPathAttributeFormatter.AddType(writer, span, constant.Type, TypeNames);
            if (constant is { Kind: TypedConstantKind.Type, IsNull: false })
            {
                XPathAttributeFormatter.AddType(writer, span, constant.Value as ITypeSymbol, ValueTypeNames);
            }
        }

        return attributes.Count is 0 ? [] : [.. attributes];
    }

    // The value of an array is the items, which are the child elements. The value of a type is its metadata name, and
    // the value of an enumeration is its underlying value.
    private static string? FormatValue(TypedConstant constant)
    {
        if (constant.IsNull)
            return null;

        return constant.Kind switch
        {
            TypedConstantKind.Array or TypedConstantKind.Error => null,
            TypedConstantKind.Type => SymbolNameFormatter.GetMetadataName(constant.Value as ITypeSymbol),
            _ => XPathAttributeFormatter.FormatConstantValue(constant.Value),
        };
    }

    private static class AttributeName
    {
        public const string Name = nameof(Name);
        public const string Position = nameof(Position);
        public const string Kind = nameof(Kind);
        public const string IsNull = nameof(IsNull);
        public const string Value = nameof(Value);
    }
}
