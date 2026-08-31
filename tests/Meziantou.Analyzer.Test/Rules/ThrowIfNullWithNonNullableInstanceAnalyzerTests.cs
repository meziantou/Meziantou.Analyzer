using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ThrowIfNullWithNonNullableInstanceAnalyzer,
    Meziantou.Analyzer.Rules.ThrowIfNullWithNonNullableInstanceFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ThrowIfNullWithNonNullableInstanceAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Theory]
    [InlineData("System.IntPtr")]
    [InlineData("System.UIntPtr")]
    [InlineData("void*")]
    [InlineData("object")]
    [InlineData("string")]
    [InlineData("int?")]
    [InlineData("System.Collections.Generic.IEnumerable<int>")]
    public Task ThrowIfNull_Ok(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            unsafe
            {
                {{type}} obj = default;
                System.ArgumentNullException.ThrowIfNull(obj);
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.Boolean")]
    [InlineData("int")]
    public Task ThrowIfNull_Diagnostic(string type)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{type}} obj = default;
            {|MA0131:System.ArgumentNullException.ThrowIfNull(obj)|};
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_Diagnostic_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            int obj = default;
            {|MA0131:System.ArgumentNullException.ThrowIfNull(obj)|};
            """;
        test.FixedCode = """
            int obj = default;

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_GenericType()
    {
        var test = CreateTest();
        test.TestCode = """
            void A<T>(T obj) => System.ArgumentNullException.ThrowIfNull(obj);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_GenericTypeWithConstraint()
    {
        var test = CreateTest();
        test.TestCode = """
            void A<T>(T obj) where T : struct => System.ArgumentNullException.ThrowIfNull(obj);
            """;

        return test.RunAsync();
    }
}
