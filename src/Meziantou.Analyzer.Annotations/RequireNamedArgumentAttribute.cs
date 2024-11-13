#pragma warning disable CS1591
#pragma warning disable IDE0060
#pragma warning disable CA1019

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates arguments must be named for this parameter.
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Parameter)]
public sealed class RequireNamedArgumentAttribute : System.Attribute
{
    public RequireNamedArgumentAttribute() { }

    public RequireNamedArgumentAttribute(bool isRequired) { }
}
