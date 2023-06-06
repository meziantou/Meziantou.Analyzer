using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseImplicitCultureSensitiveToStringAnalyzer>()
            .WithTargetFramework(TargetFramework.Net7_0);
    }

    [Theory]
    [InlineData("\"abc\"", "0f")]
    [InlineData("\"abc\"", "(float)0")]
    [InlineData("\"abc\"", "0d")]
    [InlineData("\"abc\"", "(double)0")]
    [InlineData("\"abc\"", "0m")]
    [InlineData("\"abc\"", "(decimal)0")]
    [InlineData("\"abc\"", "1f")]
    [InlineData("\"abc\"", "(float)1")]
    [InlineData("\"abc\"", "1d")]
    [InlineData("\"abc\"", "(double)1")]
    [InlineData("\"abc\"", "1m")]
    [InlineData("\"abc\"", "(decimal)1")]
    [InlineData("\"\"", "-1")]
    [InlineData("\"abc\"", "-1")]
    [InlineData("\"abc\"", "(sbyte)-1")]
    [InlineData("\"abc\"", "(short)-1")]
    [InlineData("\"abc\"", "(int)-1")]
    [InlineData("\"abc\"", "-1L")]
    [InlineData("\"abc\"", "(long)-1")]
    [InlineData("\"abc\"", "-1f")]
    [InlineData("\"abc\"", "(float)-1")]
    [InlineData("\"abc\"", "-1d")]
    [InlineData("\"abc\"", "(double)-1")]
    [InlineData("\"abc\"", "-1m")]
    [InlineData("\"abc\"", "(decimal)-1")]
    [InlineData("\"abc\"", "default(System.DateTime)")]
    [InlineData("\"abc\"", "default(System.DateTimeOffset)")]
    [InlineData("\"abc\"", "default(System.FormattableString)")]
    [InlineData("\"abc\"", "default(System.Numerics.BigInteger)")]
    [InlineData("\"abc\"", "default(System.Numerics.Complex)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector2)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector3)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector4)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector<int>)")]
    public async Task ConcatDiagnostic(string left, string right)
    {
        var sourceCode = @"
class Test
{
    void A() { _ = " + left + " + [|" + right + @"|]; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();

        var invertedSourceCode = @"
class Test
{
    void A() { _ = [|" + right + "|] + " + left + @"; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(invertedSourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("\"abc\"", "'d'")]
    [InlineData("\"abc\"", "\"def\"")]
    [InlineData("\"abc\"", "(byte)1")]
    [InlineData("\"abc\"", "1u")]
    [InlineData("\"abc\"", "(ushort)1")]
    [InlineData("\"abc\"", "1ul")]
    [InlineData("\"abc\"", "(ulong)1")]
    [InlineData("\"abc\"", "(sbyte)1")]
    [InlineData("\"abc\"", "(short)1")]
    [InlineData("\"abc\"", "1")]
    [InlineData("\"abc\"", "(int)1")]
    [InlineData("\"abc\"", "1L")]
    [InlineData("\"abc\"", "(long)1")]
    [InlineData("\"abc\"", "(long?)1")]
    [InlineData("\"abc\"", "(System.UInt128)1")]
    [InlineData("\"abc\"", "new System.Guid()")]
    [InlineData("\"abc\"", "new System.TimeSpan()")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"c\")")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"t\")")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"T\")")]
    [InlineData("\"abc\"", "new System.Uri(\"\")")]
    [InlineData("\"abc\"", @"$""test{new System.Uri("""")}""")]
    [InlineData("\"abc\"", @"' '")]
    public async Task ConcatNoDiagnostic(string left, string right)
    {
        var sourceCode = @"
class Test
{
    void A() { _ = " + left + " + " + right + @"; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();

        var invertedSourceCode = @"
class Test
{
    void A() { _ = " + right + " + " + left + @"; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(invertedSourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("abc[|{(sbyte)-1}|]")]
    [InlineData("abc[|{(short)-1}|]")]
    [InlineData("abc[|{(int)-1}|]")]
    [InlineData("abc[|{(long)-1}|]")]
    [InlineData("abc[|{(long?)-1}|]")]
    [InlineData("abc[|{(float)-1}|]")]
    [InlineData("abc[|{(double)-1}|]")]
    [InlineData("abc[|{(decimal)-1}|]")]
    [InlineData("abc[|{(float)0}|]")]
    [InlineData("abc[|{(double)0}|]")]
    [InlineData("abc[|{(decimal)0}|]")]
    [InlineData("abc[|{(float)1}|]")]
    [InlineData("abc[|{(double)1}|]")]
    [InlineData("abc[|{(decimal)1}|]")]
    [InlineData(@"test[|{new int[0].Min()}|]")]
    [InlineData(@"test[|{System.Int128.One}|]")]
    [InlineData(@"test[|{new System.DateOnly(2023,1,1)}|]")]
    public async Task InterpolatedStringDiagnostic(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc{\"def\"}")]
    [InlineData("abc{'a'}")]
    [InlineData("abc{(byte)1}")]
    [InlineData("abc{(ushort)1}")]
    [InlineData("abc{(uint)1}")]
    [InlineData("abc{(ulong)1}")]
    [InlineData(@"test{new System.Uri("""")}")]
    [InlineData(@"test{new int[0].Length}")]
    [InlineData(@"test{new int[0].Count()}")]
    [InlineData(@"test{new System.Collections.Generic.List<int>().Count}")]
    [InlineData(@"test{new System.DateOnly(2023,1,1):o}")]
    public async Task InterpolatedStringNoDiagnostic(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Theory]
    [InlineData("abc{(nint)1}")]
    public async Task InterpolatedStringNoDiagnostic_CSharp11(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task FormattableString()
    {
        var sourceCode = @"
class Test
{
    void A() { System.FormattableString a = $""abc{-1}""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString_Invariant()
    {
        var sourceCode = @"
class Test
{
    void A() { string a = System.FormattableString.Invariant($""abc{1}""); }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringConcatFormattableString()
    {
        var sourceCode = @"
class Test
{
    void A() { var a = ""abc"" + $""[|{-1}|]""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringConcat_ToString_Int32ToString()
    {
        var sourceCode = @"
class Test
{
    void ToString() { var a = ""abc"" + $""{-1}""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task StringConcat_ToString_Int32ToString_ConfigNotExcludeToString()
    {
        var sourceCode = @"
class Test
{
    void ToString() { var a = ""abc"" + $""[|{-1}|]""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .AddAnalyzerConfiguration("MA0076.exclude_tostring_methods", "false")
              .ValidateAsync();
    }

    [Fact]
    public async Task ObjectToString()
    {
        var sourceCode = @"
class Test
{
    string A() => [|new object().ToString()|];
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32ToString()
    {
        var sourceCode = @"
class Test
{
    string A() => (-1).ToString();
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SubClassToString()
    {
        var sourceCode = @"
class Test
{
    string A() => new Test().ToString();
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/516")]
    public async Task ConcatNoDiagnostic_Char()
    {
        var sourceCode = """
class Test
{
    void A()
    {
        var c = '!';
        _ = "abc" + char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}
