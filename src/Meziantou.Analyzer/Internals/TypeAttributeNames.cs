namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The names of the attributes that expose a type, such as <c>TypeMetadataName</c> for the type of a node.
/// </summary>
internal sealed record TypeAttributeNames(string Name, string MetadataName, string DocumentationId, string ReferenceId, string Kind, string IsValueType, string NullableAnnotation, string SpecialType)
{
    public string[] All { get; } = [Name, MetadataName, DocumentationId, ReferenceId, Kind, IsValueType, NullableAnnotation, SpecialType];

    /// <summary>
    /// The names of the attributes a property that returns several types exposes. The values of the other attributes
    /// cannot be joined, as a boolean or the name of a member of an enumeration is not meaningful for a list.
    /// </summary>
    public string[] List { get; } = [Name, MetadataName, DocumentationId, ReferenceId];
}
