#pragma warning disable CS1591
#pragma warning disable IDE0060

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the value of a method return value, a property, a field, or a parameter is culture insensitive,
/// even when its type is culture sensitive. This can be used to suppress rules such as <c>MA0011</c>, <c>MA0075</c>, <c>MA0076</c>.
/// <para><code>[CultureInsensitive]string CreateInvariant() => "";</code></para>
/// <para><code>[assembly: CultureInsensitive("M:Sample.StringHelper.CreateInvariant")]</code></para>
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.ReturnValue | System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class CultureInsensitiveAttribute : System.Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// This can be applied on a method, a property, a field, or a parameter to mark its value as culture insensitive.
    /// </remarks>
    public CultureInsensitiveAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveAttribute"/> class with the specified XML documentation id.
    /// </summary>
    /// <param name="xmlDocumentationId">The XML documentation id of the member to mark as culture insensitive, such as <c>M:System.Guid.ToString</c>.</param>
    /// <remarks>
    /// This can be applied on an <see cref="System.Reflection.Assembly"/> to mark the value of a member of another assembly as culture insensitive.
    /// When the id does not contain the parameter list of a method, all its overloads are marked as culture insensitive.
    /// </remarks>
    public CultureInsensitiveAttribute(string xmlDocumentationId) => XmlDocumentationId = xmlDocumentationId;

    /// <summary>
    /// Gets the XML documentation id of the member annotated at assembly level.
    /// </summary>
    /// <value>
    /// The XML documentation id of the member annotated at assembly level, or <see langword="null"/> when the attribute
    /// is applied to the member itself.
    /// </value>
    public string? XmlDocumentationId { get; }
}
