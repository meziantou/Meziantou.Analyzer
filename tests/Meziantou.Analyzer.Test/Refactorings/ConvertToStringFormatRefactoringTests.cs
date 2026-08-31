using Microsoft.CodeAnalysis;
using RefactoringTest = Meziantou.Analyzer.Test.Harness.CSharpCodeRefactoringTest<
    Meziantou.Analyzer.Refactorings.ConvertToStringFormatRefactoring>;

namespace Meziantou.Analyzer.Test.Refactorings;

public sealed class ConvertToStringFormatRefactoringTests
{
    private static RefactoringTest CreateTest()
    {
        var test = new RefactoringTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task TestRefactoring()
    {
        var test = CreateTest();
        test.TestCode = """_ = [|$"{0}"|];""";
        test.FixedCode = """_ = string.Format("{0}", 0);""";

        return test.RunAsync();
    }

    [Fact]
    public Task SimpleInterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            var name = "World";
            _ = [|$"Hello {name}"|];
            """;
        test.FixedCode = """
            var name = "World";
            _ = string.Format("Hello {0}", name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithMultipleExpressions()
    {
        var test = CreateTest();
        test.TestCode = """
            var x = 10;
            var y = 20;
            _ = [|$"X = {x}, Y = {y}"|];
            """;
        test.FixedCode = """
            var x = 10;
            var y = 20;
            _ = string.Format("X = {0}, Y = {1}", x, y);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithFormatSpecifier()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 3.14159;
            _ = [|$"Pi = {value:F2}"|];
            """;
        test.FixedCode = """
            var value = 3.14159;
            _ = string.Format("Pi = {0:F2}", value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithAlignment()
    {
        var test = CreateTest();
        test.TestCode = """
            var name = "Test";
            _ = [|$"Name: {name,10}"|];
            """;
        test.FixedCode = """
            var name = "Test";
            _ = string.Format("Name: {0,10}", name);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithAlignmentAndFormat()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 123.456;
            _ = [|$"Value: {value,10:F2}"|];
            """;
        test.FixedCode = """
            var value = 123.456;
            _ = string.Format("Value: {0,10:F2}", value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithComplexExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            var items = new[] { 1, 2, 3 };
            _ = [|$"Count: {items.Length}"|];
            """;
        test.FixedCode = """
            var items = new[] { 1, 2, 3 };
            _ = string.Format("Count: {0}", items.Length);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithEscapedBraces()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 42;
            _ = [|$"{{value}} = {value}"|];
            """;
        test.FixedCode = """
            var value = 42;
            _ = string.Format("{{value}} = {0}", value);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimInterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            var path = "file.txt";
            _ = [|@$"Path: {path}"|];
            """;
        test.FixedCode = """
            var path = "file.txt";
            _ = string.Format("Path: {0}", path);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringWithMultipleFormats()
    {
        var test = CreateTest();
        test.TestCode = """
            var date = System.DateTime.Now;
            var value = 123.456;
            _ = [|$"Date: {date:yyyy-MM-dd}, Value: {value:C2}"|];
            """;
        test.FixedCode = """
            var date = System.DateTime.Now;
            var value = 123.456;
            _ = string.Format("Date: {0:yyyy-MM-dd}, Value: {1:C2}", date, value);
            """;

        return test.RunAsync();
    }
}
