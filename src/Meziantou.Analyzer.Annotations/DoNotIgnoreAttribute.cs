#pragma warning disable CS1591
#pragma warning disable IDE0060

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the return value or the value of an <see langword="out"/> parameter must not be ignored.
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.ReturnValue | System.AttributeTargets.Parameter | System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class DoNotIgnoreAttribute : System.Attribute
{
    public DoNotIgnoreAttribute() { }

    public DoNotIgnoreAttribute(string xmlDocumentationId)
    {
        XmlDocumentationId = xmlDocumentationId;
    }

    /// <summary>Gets the XML documentation id of a method annotated at assembly level.</summary>
    public string? XmlDocumentationId { get; }

    /// <summary>Gets or sets an optional message explaining why the value must not be ignored.</summary>
    public string? Message { get; set; }
}
