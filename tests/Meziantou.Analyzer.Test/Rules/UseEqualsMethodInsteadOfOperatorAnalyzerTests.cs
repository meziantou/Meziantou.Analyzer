using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class UseEqualsMethodInsteadOfOperatorAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(Helpers.TargetFramework.Net9_0)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<UseEqualsMethodInsteadOfOperatorAnalyzer>();
    }

    [Theory]
    [InlineData("System.Net.IPAddress")]
    public async Task Report_EqualsOperator(string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                {{type}} a = null;
                {{type}} b = null;
                _ = [|a == b|];
                """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("char")]
    [InlineData("string")]
    [InlineData("sbyte")]
    [InlineData("byte")]
    [InlineData("short")]
    [InlineData("ushort")]
    [InlineData("int")]
    [InlineData("uint")]
    [InlineData("long")]
    [InlineData("ulong")]
    [InlineData("System.Int128")]
    [InlineData("System.UInt128")]
    [InlineData("System.Half")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("decimal")]
    [InlineData("System.DayOfWeek")]
    [InlineData("System.DayOfWeek?")]
    public async Task NoReport_EqualsOperator(string type)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                {{type}} a = default;
                {{type}} b = default;
                _ = a == b;
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClassWithParentEqualsMethod()
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                B a = default;
                B b = default;
                _ = a == b;

                class A
                {
                    public override bool Equals(object obj) => throw null;
                }

                class B : A
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClassWithoutEqualsMethod()
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                Sample a = default;
                Sample b = default;
                _ = a == b;

                class Sample {}
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RecordWithoutEqualsMethod()
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                Sample a = default;
                Sample b = default;
                _ = a == b; // Operator is implemented by the record

                record Sample {}
                """)
              .ValidateAsync();
    }
}
