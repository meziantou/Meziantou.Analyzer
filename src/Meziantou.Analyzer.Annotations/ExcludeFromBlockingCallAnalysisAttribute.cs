#pragma warning disable CS1591
#pragma warning disable IDE0060
#pragma warning disable CA1019

namespace Meziantou.Analyzer.Annotations;

[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class ExcludeFromBlockingCallAnalysisAttribute : System.Attribute
{
    public ExcludeFromBlockingCallAnalysisAttribute(string documentationId) { }

    public ExcludeFromBlockingCallAnalysisAttribute(System.Type containingType, string memberName) { }

    public ExcludeFromBlockingCallAnalysisAttribute(System.Type containingType, string memberName, params System.Type[] parameterTypes) { }
}
