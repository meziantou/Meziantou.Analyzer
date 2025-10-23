#if ROSLYN_4_10_OR_GREATER
using Meziantou.Analyzer.Suppressors;
using Microsoft.CodeAnalysis;
using TestHelper;

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

    [Fact]
    public async Task System_IO_Stream_Seek_0_End()
        => await CreateProjectBuilder()
            .WithSourceCode("""
                using System.IO;
                static void A()
                {
                    Stream stream = null;
                    [||]stream.Seek(0, SeekOrigin.End);
                }
                """)
            .ValidateAsync();

    [Fact]
    public async Task System_IO_Stream_Seek_0_Begin()
        => await CreateProjectBuilder()
            .WithSourceCode("""
                using System.IO;
                static void A()
                {
                    Stream stream = null;
                    stream.Seek(0, SeekOrigin.Begin);
                }
                """)
            .ValidateAsync();

    [Fact]
    public async Task System_Collections_Generic_HashSet_Add()
        => await CreateProjectBuilder()
            .WithSourceCode("""
                using System.IO;
                static void A()
                {
                    System.Collections.Generic.HashSet<int> a = null;
                    a.Add(0);
                }
                """)
            .ValidateAsync();
}
#endif
