namespace Meziantou.Analyzer.Internals;

/// <summary>
/// An element of an <see cref="AttributeDataForest"/>: an attribute applied to a symbol, one of its arguments, or an
/// item of an argument that is an array.
/// </summary>
internal sealed class AttributeDataElement(string name, AttributeData attribute, TypedConstant constant, string? argumentName, int position)
{
    public const string AttributeDataName = "AttributeData";
    public const string ConstructorArgumentName = "ConstructorArgument";
    public const string NamedArgumentName = "NamedArgument";
    public const string ItemName = "Item";

    /// <summary>The name of the element, such as <c>AttributeData</c> or <c>ConstructorArgument</c>.</summary>
    public string Name { get; } = name;

    /// <summary>The attribute the element is, or the attribute that contains the argument the element is.</summary>
    public AttributeData Attribute { get; } = attribute;

    /// <summary>The value of the argument or of the item. It is the default value for an attribute.</summary>
    public TypedConstant Constant { get; } = constant;

    /// <summary>The name of a named argument.</summary>
    public string? ArgumentName { get; } = argumentName;

    /// <summary>The position of a constructor argument or of an item. It is -1 for the other elements.</summary>
    public int Position { get; } = position;

    public bool IsAttribute => string.Equals(Name, AttributeDataName, StringComparison.Ordinal);

    public AttributeDataElement? Parent { get; set; }

    public AttributeDataElement[] Children { get; set; } = [];

    public int IndexInParent { get; set; }

    internal XPathAttribute[]? Attributes { get; set; }
}
