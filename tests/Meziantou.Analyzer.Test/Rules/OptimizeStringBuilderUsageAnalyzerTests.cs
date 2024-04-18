using System.Collections.Generic;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeStringBuilderUsageAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeStringBuilderUsageAnalyzer>()
            .WithCodeFixProvider<OptimizeStringBuilderUsageFixer>();
    }

    [Fact]
    public async Task AppendFormat_NoDiagnostic()
    {
        const string SourceCode = @"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(""{10}"", 10);
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("10")]
    [InlineData("10 + 20")]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    [InlineData(@"$""abc{""test""}""")]
    [InlineData(@"""abc"" + ""test""")]
    [InlineData(@"$""abc{""test""}"" + ""test""")]
    public async Task Append_NoDiagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(" + text + @");
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"""""")]
    [InlineData(@""""" + """"")]
    [InlineData(@""""".Substring(0, 10)")]
    public async Task Append_ReportDiagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Append(" + text + @");
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    public async Task AppendLine_NoDiagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine(" + text + @");
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"10.ToString()")]
    public async Task AppendLine_ReportDiagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(" + text + @");
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"string.Format(""{0}"", 0)")]
    public async Task Insert_NoDiagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, " + text + @");
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"10.ToString()")]
    public async Task Insert_Diagnostic(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Insert(0, " + text + @");
    }
}")
              .ValidateAsync();
    }

    public static IEnumerable<object[]> EmptyStringsArguments
    {
        get
        {
            yield return new object[] { @"$""""" };
            yield return new object[] { @"$""{""""}""" };
            yield return new object[] { @"""""" };
            yield return new object[] { @""""" + """"" };
            yield return new object[] { @"string.Empty" };
        }
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public async Task AppendLine_EmptyString(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(" + text + @");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public async Task Append_EmptyString(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Append(" + text + @").AppendLine();
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public async Task Insert_EmptyString(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Insert(0, " + text + @").AppendLine();
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""a""")]
    public async Task Append_OneCharString(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append([||]" + text + @");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('a');
    }
}")
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""a""")]
    public async Task Insert_OneCharString(string text)
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, [||]" + text + @");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Insert(0, 'a');
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_InterpolatedString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Append($""A{1}BC{2:X2}DEF{1,-2:N2}"");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).Append(""DEF"").AppendFormat(""{0,-2:N2}"", 1);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_InterpolatedString_FinishWithString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine($""A{1}BC{2:X2}DEF"");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).AppendLine(""DEF"");
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_InterpolatedString_FinishWithChar()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine($""A{1}BC{2:X2}D"");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).Append('D').AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_InterpolatedString_FinishWithObject()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine($""A{1}BC{2:X2}"");
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append('A').Append(1).Append(""BC"").AppendFormat(""{0:X2}"", 2).AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_StringAdd()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        [||]new StringBuilder().Append(""ab"" + a);
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        new StringBuilder().Append(""ab"").Append(a);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_StringAdd()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        [||]new StringBuilder().AppendLine(""ab"" + a);
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        var a = """";
        new StringBuilder().Append(""ab"").AppendLine(a);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_ToString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().Append(1.ToString());
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(1);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_ToString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(1.ToString());
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(1).AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_ToStringWithFormatAndCulture()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(1.ToString(""N"", null));
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_AppendFormat_Variable()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A(string format)
    {
        new StringBuilder().Append(1.ToString(format, null));
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_StringFormat_AppendFormat()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A(string format)
    {
        [||]new StringBuilder().Append(string.Format(""{0:N2}-{1:N0}"", 1, 2));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A(string format)
    {
        new StringBuilder().AppendFormat(""{0:N2}-{1:N0}"", 1, 2);
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_AppendFormat()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(string.Format(null, ""{0:N}"", 1));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendFormat(null, ""{0:N}"", 1).AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_StringJoin_AppendJoin_OldTargetFramework()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A(string format)
    {
        new StringBuilder().Append(string.Join("", "", new[] { 1, 2, 3 }));
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Append_StringJoin_AppendJoin()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A(string format)
    {
        [||]new StringBuilder().Append(string.Join("", "", new[] { 1, 2, 3 }));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A(string format)
    {
        new StringBuilder().AppendJoin("", "", new[] { 1, 2, 3 });
    }
}")
              .WithTargetFramework(TargetFramework.Net6_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_AppendJoin()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(string.Join("", "", new[] { 1, 2, 3 }));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().AppendJoin("", "", new[] { 1, 2, 3 }).AppendLine();
    }
}")
              .WithTargetFramework(TargetFramework.Net6_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_AppendSubString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine("""".Substring(0, 1));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append("""", 0, 1).AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_AppendSubStringWithoutLength()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
class Test
{
    void A()
    {
        [||]new StringBuilder().AppendLine(""abc"".Substring(2));
    }
}")
              .ShouldFixCodeWith(@"using System.Text;
class Test
{
    void A()
    {
        new StringBuilder().Append(""abc"", 2, ""abc"".Length - 2).AppendLine();
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task AppendLine_CustomStructToString()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Text;
struct MyStruct
{
}

class Test
{
    void A()
    {
        new StringBuilder().AppendLine(new MyStruct().ToString());
    }
}")
              .ValidateAsync();
    }
}
