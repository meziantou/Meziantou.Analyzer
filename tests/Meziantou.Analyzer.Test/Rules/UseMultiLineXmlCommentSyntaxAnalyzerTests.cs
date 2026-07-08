using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseMultiLineXmlCommentSyntaxAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseMultiLineXmlCommentSyntaxAnalyzer>()
            .WithCodeFixProvider<UseMultiLineXmlCommentSyntaxFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task SummarySingleLine_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0211:<summary>description</summary>|}
                  class Sample { }
                  """)
              .ShouldFixCodeWith("""
                  /// <summary>
                  /// description
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SingleLineSummaryWithNestedElement_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0211:<summary>This has <c>code</c> inside</summary>|}
                  class Sample { }
                  """)
              .ShouldFixCodeWith("""
                  /// <summary>
                  /// This has <c>code</c> inside
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SingleLineSummaryWithCData_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0211:<summary><![CDATA[Sample]]></summary>|}
                  class Sample { }
                  """)
              .ShouldFixCodeWith("""
                  /// <summary>
                  /// <![CDATA[Sample]]>
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task EmptyContent_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <summary></summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ParamSingleLine_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// <param name="value">The value</param>
                      public void Method(int value) { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task SummaryContainingOnlyWhitespace_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <summary>   </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiLineDescription_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <summary>
                  /// description
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiLineSummaryWithNestedElement_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <summary>
                  /// This has <c>code</c> inside
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiLineSummaryWithCData_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <summary>
                  /// <![CDATA[Sample]]>
                  /// </summary>
                  class Sample { }
                  """)
              .ValidateAsync();
    }
}
