#pragma warning disable CS1591
#pragma warning disable IDE0060

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the return value or the value of an <see langword="out"/> parameter must not be ignored.
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.ReturnValue | System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class DoNotIgnoreAttribute : System.Attribute
{
    public DoNotIgnoreAttribute() { }

    /// <summary>Gets or sets an optional message explaining why the value must not be ignored.</summary>
    public string? Message { get; set; }
}
