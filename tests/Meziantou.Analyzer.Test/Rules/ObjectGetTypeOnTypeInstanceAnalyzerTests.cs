using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class ObjectGetTypeOnTypeInstanceAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithAnalyzer<ObjectGetTypeOnTypeInstanceAnalyzer>();
    }

    [Theory]
    [InlineData("new object().GetType();")]
    [InlineData("string.Empty.GetType();")]
    [InlineData("12.GetType();")]
    [InlineData("System.Type.GetType(\"\");")]
    public async Task Valid(string code)
    {
        await CreateProjectBuilder()
            .WithSourceCode(code)
            .ValidateAsync();
    }

    [Fact]
    public async Task AbstractClass_Valid()
    {
        const string SourceCode = """
                abstract class Test
                {
                    public Test(int a) { }
                };
            """;

        await CreateProjectBuilder().WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Theory]
    [InlineData("new object().GetType().GetType();")]
    [InlineData("((System.Type)null).GetType();")]
    [InlineData("default(System.Type).GetType();")]
    public async Task Invalid(string code)
    {
        await CreateProjectBuilder()
              .WithSourceCode(code)
              .ValidateAsync();
    }

}
