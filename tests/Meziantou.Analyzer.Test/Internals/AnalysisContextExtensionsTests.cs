using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class AnalysisContextExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("dummy")]
    [InlineData("truthy")]
    public void GetAdditionalFlags_NotEnabled(string? value)
    {
        Assert.Equal(GeneratedCodeAnalysisFlags.None, AnalysisContextExtensions.GetAdditionalFlags(value));
    }

    [Theory]
    [InlineData("1")]
    [InlineData(" 1 ")]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("  true  ")]
    public void GetAdditionalFlags_Enabled(string value)
    {
        Assert.Equal(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics, AnalysisContextExtensions.GetAdditionalFlags(value));
    }
}
