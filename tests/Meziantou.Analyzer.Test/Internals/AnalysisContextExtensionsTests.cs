using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test.Internals;

public sealed class AnalysisContextExtensionsTests
{
    [Theory]
    [InlineData("0")]
    [InlineData(" 0 ")]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("  false  ")]
    public void GetAdditionalFlags_OptedOut(string value)
    {
        Assert.Equal(GeneratedCodeAnalysisFlags.None, AnalysisContextExtensions.GetAdditionalFlags(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("dummy")]
    [InlineData("falsy")]
    public void GetAdditionalFlags_NotOptedOut(string? value)
    {
        Assert.Equal(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics, AnalysisContextExtensions.GetAdditionalFlags(value));
    }
}
