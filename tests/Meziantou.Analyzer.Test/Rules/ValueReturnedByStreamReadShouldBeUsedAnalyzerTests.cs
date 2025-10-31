using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ValueReturnedByStreamReadShouldBeUsedAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ValueReturnedByStreamReadShouldBeUsedAnalyzer>();
    }

    [Fact]
    public async Task Read_ReturnValueNotUsed()
    {
        const string SourceCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var stream = File.OpenRead("""");
                    [||]stream.Read(null, 0, 0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadAsync_ReturnValueNotUsed()
    {
        const string SourceCode = """
            using System.IO;
            class Test
            {
                async void A()
                {
                    var stream = File.OpenRead("""");
                    await [||]stream.ReadAsync(null, 0, 0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadAsync_ReturnValueUsed_DiscardOperator()
    {
        const string SourceCode = """
            using System.IO;
            class Test
            {
                async void A()
                {
                    var stream = File.OpenRead("""");
                    _ = await stream.ReadAsync(null, 0, 0);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Read_ReturnValueUsed_MethodCall()
    {
        const string SourceCode = """
            using System.IO;
            class Test
            {
                async void A()
                {
                    var stream = File.OpenRead("""");
                    System.Console.Write(stream.Read(null, 0, 0));
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
