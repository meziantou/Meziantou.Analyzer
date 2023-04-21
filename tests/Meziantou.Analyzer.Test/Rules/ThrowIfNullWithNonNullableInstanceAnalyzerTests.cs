using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class ThrowIfNullWithNonNullableInstanceAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .WithTargetFramework(TargetFramework.Net7_0)
            .WithAnalyzer<ThrowIfNullWithNonNullableInstanceAnalyzer>();
    }

    [Theory]
    [InlineData("System.IntPtr")]
    [InlineData("System.UIntPtr")]
    [InlineData("void*")]
    [InlineData("object")]
    [InlineData("string")]
    [InlineData("int?")]
    [InlineData("System.Collections.Generic.IEnumerable<int>")]
    public async Task ThrowIfNull_Ok(string type)
    {
        var sourceCode = $$"""
            unsafe
            {
                {{type}} obj = default;
                System.ArgumentNullException.ThrowIfNull(obj);
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Boolean")]
    [InlineData("int")]
    public async Task ThrowIfNull_Diagnostic(string type)
    {
        var sourceCode = $$"""
            {{type}} obj = default;
            [||]System.ArgumentNullException.ThrowIfNull(obj);
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}
