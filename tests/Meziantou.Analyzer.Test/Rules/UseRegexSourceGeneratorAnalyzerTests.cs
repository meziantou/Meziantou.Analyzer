using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public class UseRegexSourceGeneratorAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net7_0)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
            .WithAnalyzer<UseRegexSourceGeneratorAnalyzer>()
            .WithCodeFixProvider<UseRegexSourceGeneratorFixer>()
            .WithNoFixCompilation(); // requires the regex source generator
    }

    [Fact]
    public async Task NewRegex_Options_Timeout()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    Regex a = [||]new Regex(""testpattern"", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    Regex a = MyRegex();

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewRegex_Options()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    Regex a = [||]new Regex(""testpattern"", RegexOptions.ExplicitCapture);
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    Regex a = MyRegex();

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task NewRegex()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    Regex a = [||]new Regex(""testpattern"");
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    Regex a = MyRegex();

    [GeneratedRegex(""testpattern"")]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexIsMatch_Options_Timeout()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    bool a = [||]Regex.IsMatch(""test"", ""testpattern"", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    bool a = MyRegex().IsMatch(""test"");

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexIsMatch_Options()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    bool a = [||]Regex.IsMatch(""test"", ""testpattern"", RegexOptions.ExplicitCapture);
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    bool a = MyRegex().IsMatch(""test"");

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexIsMatch()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    bool a = [||]Regex.IsMatch(""test"", ""testpattern"");
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    bool a = MyRegex().IsMatch(""test"");

    [GeneratedRegex(""testpattern"")]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexReplace_Options_Timeout()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    string a = [||]Regex.Replace(""test"", ""testpattern"", ""newValue"", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    string a = MyRegex().Replace(""test"", ""newValue"");

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexReplace_Options()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    string a = [||]Regex.Replace(""test"", ""testpattern"", ""newValue"", RegexOptions.ExplicitCapture);
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    string a = MyRegex().Replace(""test"", ""newValue"");

    [GeneratedRegex(""testpattern"", RegexOptions.ExplicitCapture)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexReplace()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    string a = [||]Regex.Replace(""test"", ""testpattern"", ""newValue"");
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    string a = MyRegex().Replace(""test"", ""newValue"");

    [GeneratedRegex(""testpattern"")]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task RegexReplace_MatchEvaluator()
    {
        const string SourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    string a = [||]Regex.Replace(""test"", ""testpattern"", evaluator: match => """");
}
";

        const string CodeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    string a = MyRegex().Replace(""test"", evaluator: match => """");

    [GeneratedRegex(""testpattern"")]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("TimeSpan.FromMilliseconds(10)", "10")]
    [InlineData("TimeSpan.FromSeconds(10.5)", "10500")]
    [InlineData("TimeSpan.FromMinutes(1)", "60000")]
    [InlineData("TimeSpan.FromHours(1)", "3600000")]
    [InlineData("TimeSpan.FromDays(1)", "86400000")]
    [InlineData("TimeSpan.Zero", "0")]
    [InlineData("new TimeSpan(10000)", "1")]
    [InlineData("new TimeSpan(1, 2, 3)", "3723000")]
    [InlineData("new TimeSpan(1, 2, 3, 4)", "93784000")]
    [InlineData("new TimeSpan(1, 2, 3, 4, 5)", "93784005")]
    public async Task Timeout(string timeout, string milliseconds)
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    Regex a = [||]new Regex(""testpattern"", RegexOptions.None, " + timeout + @");
}
";

        var codeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    Regex a = MyRegex();

    [GeneratedRegex(""testpattern"", RegexOptions.None, matchTimeoutMilliseconds: " + milliseconds + @")]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(codeFix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Threading.Timeout.InfiniteTimeSpan")]
    [InlineData("Regex.InfiniteMatchTimeout")]
    public async Task New_Timeout_Infinite(string timeout)
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    Regex a = [||]new Regex(""testpattern"", RegexOptions.None, " + timeout + @");
}
";

        var codeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    Regex a = MyRegex();

    [GeneratedRegex(""testpattern"", RegexOptions.None, matchTimeoutMilliseconds: -1)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(codeFix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Threading.Timeout.InfiniteTimeSpan")]
    [InlineData("Regex.InfiniteMatchTimeout")]
    public async Task Static_Timeout_Infinite(string timeout)
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    bool a = [||]Regex.IsMatch(""input"", ""testpattern"", RegexOptions.None, " + timeout + @");
}
";

        var codeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    bool a = MyRegex().IsMatch(""input"");

    [GeneratedRegex(""testpattern"", RegexOptions.None, matchTimeoutMilliseconds: -1)]
    private static partial Regex MyRegex();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(codeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenerateUniqueMethodName()
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    bool a = [||]Regex.IsMatch(""input"", ""testpattern"");

    private static Regex MyRegex() => throw null;
}
";

        var codeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Test
{
    bool a = MyRegex_().IsMatch(""input"");

    private static Regex MyRegex() => throw null;
    [GeneratedRegex(""testpattern"")]
    private static partial Regex MyRegex_();
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(codeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonConstantPattern()
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Test
{
    void A(string pattern) => Regex.IsMatch(""input"", pattern);
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedTypeShouldAddPartialToAllAncestorTypes()
    {
        var sourceCode = @"
using System;
using System.Text.RegularExpressions;

class Sample
{
    private partial class Inner1
    {
        class Inner
        {
            bool a = [||]Regex.IsMatch(""input"", ""testpattern"");
        }
    }
}
";

        var codeFix = @"
using System;
using System.Text.RegularExpressions;

partial class Sample
{
    private partial class Inner1
    {
        partial class Inner
        {
            bool a = MyRegex().IsMatch(""input"");

            [GeneratedRegex(""testpattern"")]
            private static partial Regex MyRegex();
        }
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(codeFix)
              .ValidateAsync();
    }
}
