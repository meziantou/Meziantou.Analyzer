using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringCreateInsteadOfFormattableStringAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseStringCreateInsteadOfFormattableStringAnalyzer>()
            .WithCodeFixProvider<UseStringCreateInsteadOfFormattableStringFixer>();
    }

    [Fact]
    public async Task Net5_NoDiagnostic()
    {
        const string SourceCode = """
using System;
class TypeName
{
    public void Test()
    {
        FormattableString.Invariant($"");
        FormattableString.CurrentCulture($"");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString_NoDiagnostic()
    {
        const string SourceCode = """
using System;
class TypeName
{
    public void Test()
    {
        FormattableString fs = default;
        FormattableString.Invariant(fs);
        FormattableString.CurrentCulture(fs);
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableStringInvariant()
    {
        const string SourceCode = """
using System;
class TypeName
{
    public void Test()
    {
        [|FormattableString.Invariant($"")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        string.Create(CultureInfo.Invariant, $"");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task FormattableStringCurrentCulture()
    {
        const string SourceCode = """
using System;
class TypeName
{
    public void Test()
    {
        [|FormattableString.CurrentCulture($"")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        string.Create(CultureInfo.CurrentCulture, $"");
    }
}
""";
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

}
