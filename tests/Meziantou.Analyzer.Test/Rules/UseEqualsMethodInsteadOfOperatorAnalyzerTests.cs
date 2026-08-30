using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseEqualsMethodInsteadOfOperatorAnalyzer,
    Meziantou.Analyzer.Rules.UseEqualsMethodInsteadOfOperatorFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseEqualsMethodInsteadOfOperatorAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Theory]
    [InlineData("System.Net.IPAddress")]
    public Task Report_EqualsOperator(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{type}} a = null;
            {{type}} b = null;
            _ = [|a == b|];
            """;
        test.FixedCode = $$"""
            {{type}} a = null;
            {{type}} b = null;
            _ = object.Equals(a, b);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Report_NotEqualsOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            System.Net.IPAddress a = null;
            System.Net.IPAddress b = null;
            _ = [|a != b|];
            """;
        test.FixedCode = """
            System.Net.IPAddress a = null;
            System.Net.IPAddress b = null;
            _ = !object.Equals(a, b);
            """;

        return test.RunAsync();
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
    public Task NoReport_EqualsOperator(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{type}} a = default;
            {{type}} b = default;
            _ = a == b;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithParentEqualsMethod()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClassWithoutEqualsMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = default;
            Sample b = default;
            _ = a == b;

            class Sample {}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordWithoutEqualsMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = default;
            Sample b = default;
            _ = a == b; // Operator is implemented by the record

            record Sample {}
            """;

        return test.RunAsync();
    }
}
