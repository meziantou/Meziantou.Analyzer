#pragma warning disable CS1591
#pragma warning disable IDE0060

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the value of a property, a field, or a parameter is culture insensitive, even when its type is
/// culture sensitive. This can be used to suppress rules such as <c>MA0011</c>, <c>MA0075</c>, <c>MA0076</c>.
/// <para><code>[CultureInsensitive]double Value { get; }</code></para>
/// <para><code>[assembly: CultureInsensitive("P:Sample.StringHelper.InvariantValue")]</code></para>
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class CultureInsensitiveAttribute : System.Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// This can be applied on a property, a field, or a parameter to mark its value as culture insensitive.
    /// </remarks>
    public CultureInsensitiveAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveAttribute"/> class with the specified XML documentation id.
    /// </summary>
    /// <param name="xmlDocumentationId">The XML documentation id of the property or the field to mark as culture insensitive, such as <c>P:System.DateTime.Now</c>.</param>
    /// <remarks>
    /// This can be applied on an <see cref="System.Reflection.Assembly"/> to mark the value of a property or a field of another assembly as culture insensitive.
    /// </remarks>
    public CultureInsensitiveAttribute(string xmlDocumentationId) => XmlDocumentationId = xmlDocumentationId;

    /// <summary>
    /// Gets the XML documentation id of the property or the field annotated at assembly level.
    /// </summary>
    /// <value>
    /// The XML documentation id of the property or the field annotated at assembly level, or <see langword="null"/> when
    /// the attribute is applied to the member itself.
    /// </value>
    public string? XmlDocumentationId { get; }
}
