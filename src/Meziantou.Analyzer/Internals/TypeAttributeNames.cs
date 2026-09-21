namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The names of the attributes that expose a type, such as <c>TypeMetadataName</c> for the type of a node.
/// </summary>
internal sealed record TypeAttributeNames(string Name, string MetadataName, string DocumentationId, string ReferenceId, string IsValueType, string NullableAnnotation, string SpecialType)
{
    public string[] All { get; } = [Name, MetadataName, DocumentationId, ReferenceId, IsValueType, NullableAnnotation, SpecialType];
}
