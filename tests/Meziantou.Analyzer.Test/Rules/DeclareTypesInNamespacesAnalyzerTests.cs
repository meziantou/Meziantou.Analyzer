using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DeclareTypesInNamespacesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DeclareTypesInNamespacesAnalyzer>();
    }

    [Fact]
    public async Task InNamespace_IsValid()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
namespace Test
{
    class Sample
    {
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task NotInNamespace_IsInvalid()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class [||]Sample
{
    class Nested { }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelProgram_9()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
              .WithOutputKind(OutputKind.ConsoleApplication)
              .WithSourceCode(@"
System.Console.WriteLine();")
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task TopLevelProgram_10()
    {
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithOutputKind(OutputKind.ConsoleApplication)
              .WithSourceCode(@"
System.Console.WriteLine();")
              .ValidateAsync();
    }
#endif
}
