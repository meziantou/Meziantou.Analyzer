using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseRegexTimeoutAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RegexMethodUsageAnalyzer>()
            .WithAnalyzer<GeneratedRegexAttributeUsageAnalyzer>();
    }

    [Fact]
    public async Task IsMatch_MissingTimeout_ShouldReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        [||]Regex.IsMatch(""test"", ""[a-z]+"");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IsMatch_WithTimeout_ShouldNotReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IsMatch_NonBacktracking_WithoutTimeout_ShouldNotReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", ""[a-z]+"", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking);
    }
}";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor_MissingTimeout_ShouldReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        [||]new Regex(""[a-z]+"");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor_WithTimeout_ShouldNotReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.ExplicitCapture, System.TimeSpan.FromSeconds(1));
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor_WithoutTimeout_NonBacktracking_ShouldNotReportError()
    {
        const string SourceCode = @"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""[a-z]+"", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.Net7_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonRegexCtor_ShouldNotReportError()
    {
        const string SourceCode = @"
class TestClass
{
    void Test()
    {
        new System.Exception("""");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegex_WithoutTimeout()
    {
        var project = CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode("""
            using System.Text.RegularExpressions;
            partial class TestClass
            {
                [[||][||]GeneratedRegex("pattern", RegexOptions.None)]
                private static partial Regex Test();
            }
            partial class TestClass
            {
                [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Text.RegularExpressions.Generator", "7.0.8.6910")]
                private static partial Regex Test() => throw null;
            }
            """);

        await project.ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegex_WithTimeout()
    {
        var project = CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(@"using System.Text.RegularExpressions;
partial class TestClass
{
    [GeneratedRegex(""pattern"", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Test();
}
partial class TestClass
{
    private static partial Regex Test() => throw null;
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegex_WithoutTimeout_NonBacktracking()
    {
        var project = CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(@"using System.Text.RegularExpressions;
partial class TestClass
{
    [GeneratedRegex(""pattern"", RegexOptions.NonBacktracking)]
    private static partial Regex Test();
}
partial class TestClass
{
    private static partial Regex Test() => throw null;
}");

        await project.ValidateAsync();
    }
}
