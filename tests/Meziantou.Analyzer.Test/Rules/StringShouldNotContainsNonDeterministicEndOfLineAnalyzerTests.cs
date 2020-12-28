using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public class StringShouldNotContainsNonDeterministicEndOfLineAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
                .WithAnalyzer<StringShouldNotContainsNonDeterministicEndOfLineAnalyzer>()
                .WithCodeFixProvider<StringShouldNotContainsNonDeterministicEndOfLineFixer>();
        }

        [Fact]
        public async Task Valid()
        {
            const string SourceCode = @"
class Dummy
{
    void Test()
    {
        _ = ""test"";
        _ = $""test"";
        _ = ""test\r\nabc"";
        _ = $""test{0}\r\nabc"";
        _ = @""test"";
        _ = $@""test{0}"";
        _ = $@""test{
0}"";
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task VerbatimString()
        {
            const string SourceCode = @"
class Dummy
{
    void Test()
    {
        _ = [|@""line1
line2""|];
    }
}
";

            const string CodeFix = @"
class Dummy
{
    void Test()
    {
        _ = ""line1\n"" +
            ""line2"";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(1, CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task VerbatimString2()
        {
            const string SourceCode = @"
class Dummy
{
    void Test()
    {
        _ = [|@""line1\t
line2""|];
    }
}
";

            const string CodeFix = @"
class Dummy
{
    void Test()
    {
        _ = ""line1\\t\r\n"" +
            ""line2"";
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(2, CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task VerbatimInterpolatedString()
        {
            const string SourceCode = @"
class Dummy
{
    void Test()
    {
        _ = [|$@""line1{0}
line2""|];
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
