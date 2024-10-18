#if ROSLYN_4_10_OR_GREATER
using System.Threading.Tasks;
using Meziantou.Analyzer.Suppressors;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Suppressors;
public sealed class IDE0058SuppressorTests
{
    private static ProjectBuilder CreateProjectBuilder()
        => new ProjectBuilder()
            .WithMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers("IDE0058")
            .WithAnalyzer<IDE0058Suppressor>()
            .WithOutputKind(OutputKind.ConsoleApplication);

    // Ensure the diagnostic is reported without the suppressor
    [Fact]
    public async Task IDE0058IsReported()
        => await new ProjectBuilder()
            .WithMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers("IDE0058")
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithSourceCode("""
                static void A()
                {
                    [|new System.Text.StringBuilder().Append("Hello")|];
                    [|System.IO.Directory.CreateDirectory("dir")|];
                }
                """)
            .ValidateAsync();

    [Fact]
    public async Task StringBuilder_Append()
        => await CreateProjectBuilder()
            .WithSourceCode("""
                static void A()
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Hello");
                    System.Console.WriteLine(sb.ToString());
                }
                """)
            .ValidateAsync();

    [Fact]
    public async Task Directory_CreateDirectory()
        => await CreateProjectBuilder()
            .WithSourceCode("""
                static void A()
                {
                    System.IO.Directory.CreateDirectory("dir");
                }
                """)
            .ValidateAsync();
}
#endif