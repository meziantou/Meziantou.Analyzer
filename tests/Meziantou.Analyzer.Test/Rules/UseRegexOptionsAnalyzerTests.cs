using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseRegexOptionsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithFrameworkSourceGenerators()
            .WithAnalyzer<RegexMethodUsageAnalyzer>()
            .WithAnalyzer<GeneratedRegexAttributeUsageAnalyzer>()
            .WithCodeFixProvider<UseRegexExplicitCaptureOptionsFixer>();
    }

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
    [InlineData("([a-z]+)", "RegexOptions.None", false)]
    [InlineData("([a-z]+)", "RegexOptions.ExplicitCapture", true)]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.None", true)]
    public async Task IsMatch_RegexOptions(string regex, string options, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        Regex.IsMatch(""test"", """ + regex + @""", " + (isValid ? "" : "[|") + options + (isValid ? "" : "|]") + @", default);
    }
}");
        await project.ValidateAsync();
    }

    [Fact]
    public async Task IsMatch_RegexOptions_CodeFix()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Text.RegularExpressions;
                  class TestClass
                  {
                      void Test()
                      {
                          Regex.IsMatch("test", "([a-z]+)", [|RegexOptions.None|], default);
                      }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System.Text.RegularExpressions;
                  class TestClass
                  {
                      void Test()
                      {
                          Regex.IsMatch("test", "([a-z]+)", RegexOptions.None | RegexOptions.ExplicitCapture, default);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", false)]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase", true)]
    [InlineData("[a-z]+", "RegexOptions.ECMAScript", true)]
    [InlineData("([a-z]+)", "RegexOptions.ECMAScript", true)]
    public async Task Ctor_RegexOptions(string regex, string options, bool isValid)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode(@"using System.Text.RegularExpressions;
class TestClass
{
    void Test()
    {
        new Regex(""" + regex + @""", " + (isValid ? "" : "[|") + options + (isValid ? "" : "|]") + @", default);
    }
}");

        await project.ValidateAsync();
    }

    [Theory]
    [InlineData("(?<test>[a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase")]
    [InlineData("[a-z]+", "RegexOptions.ECMAScript")]
    [InlineData("([a-z]+)", "RegexOptions.ECMAScript")]
    public async Task GeneratedRegex_RegexOptions_Valid(string regex, string options)
    {
        var project = CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(@"using System.Text.RegularExpressions;
partial class TestClass
{
    [GeneratedRegex(""" + regex + @""", " + options + @", 0)]
    private static partial Regex Test();

    private static partial Regex Test() => throw null;
}");

        await project.ValidateAsync();
    }

    [Theory]
    [InlineData("([a-z]+)", "RegexOptions.CultureInvariant | RegexOptions.IgnoreCase")]
    public async Task GeneratedRegex_RegexOptions_Invalid(string regex, string options)
    {
        var project = CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(@"using System.Text.RegularExpressions;
partial class TestClass
{
    [[|GeneratedRegex(""" + regex + @""", " + options + @", 0)|]]
    private static partial Regex Test();
}
partial class TestClass
{
    private static partial Regex Test() => throw null;
}");

        await project.ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegex_RegexOptions_Invalid_CodeFix()
    {
        var project = new ProjectBuilder()
              .WithAnalyzer<GeneratedRegexAttributeUsageAnalyzer>()
              .WithCodeFixProvider<UseRegexExplicitCaptureOptionsFixer>()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode("""
                  using System.Text.RegularExpressions;
                  partial class TestClass
                  {
                      [[|GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 0)|]]
                      private static partial Regex Test();
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test() => throw null;
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System.Text.RegularExpressions;
                  partial class TestClass
                  {
                      [GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 0)]
                      private static partial Regex Test();
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test() => throw null;
                  }
                  """);

        await project.ValidateAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public async Task GeneratedRegexProperty_RegexOptions_Valid()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode("""
                  using System.Text.RegularExpressions;

                  partial class TestClass
                  {
                      [GeneratedRegex("(?<test>[a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 0)]
                      private static partial Regex Test { get; }
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test { get => throw null; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegexProperty_RegexOptions_Invalid()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode("""
                  using System.Text.RegularExpressions;

                  partial class TestClass
                  {
                      [[|GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 0)|]]
                      private static partial Regex Test { get; }
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test { get => throw null; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task GeneratedRegexProperty_RegexOptions_Invalid_CodeFix()
    {
        await new ProjectBuilder()
              .WithAnalyzer<GeneratedRegexAttributeUsageAnalyzer>()
              .WithCodeFixProvider<UseRegexExplicitCaptureOptionsFixer>()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
              .WithTargetFramework(TargetFramework.Net9_0)
              .WithSourceCode("""
                  using System.Text.RegularExpressions;

                  partial class TestClass
                  {
                      [[|GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 0)|]]
                      private static partial Regex Test { get; }
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test { get => throw null; }
                  }
                  """)
              .ShouldFixCodeWith("""
                  using System.Text.RegularExpressions;

                  partial class TestClass
                  {
                      [GeneratedRegex("([a-z]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, 0)]
                      private static partial Regex Test { get; }
                  }
                  partial class TestClass
                  {
                      private static partial Regex Test { get => throw null; }
                  }
                  """)
              .ValidateAsync();
    }
#endif
}
