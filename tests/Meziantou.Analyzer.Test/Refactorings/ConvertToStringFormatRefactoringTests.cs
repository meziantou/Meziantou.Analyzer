using Meziantou.Analyzer.Refactorings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Refactorings;

public sealed class ConvertToStringFormatRefactoringTests
{
    internal static async Task RunRefactoringAsync<TCodeRefactoring>(
        string source,
        string fixedSource)
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        var test = new CSharpCodeRefactoringTest<TCodeRefactoring, DefaultVerifier>
        {
            CodeActionIndex = 0,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };

        test.TestState.Sources.Add(source);
        test.FixedState.Sources.Add(fixedSource);

        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestState.AdditionalReferences.Add(typeof(TCodeRefactoring).Assembly);

        await test.RunAsync();
    }

    [Fact]
    public async Task TestRefactoring()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """_ = [|$"{0}"|];""",
            """_ = string.Format("{0}", 0);""");
    }

    [Fact]
    public async Task SimpleInterpolatedString()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var name = "World";
            _ = [|$"Hello {name}"|];
            """,
            """
            var name = "World";
            _ = string.Format("Hello {0}", name);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithMultipleExpressions()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var x = 10;
            var y = 20;
            _ = [|$"X = {x}, Y = {y}"|];
            """,
            """
            var x = 10;
            var y = 20;
            _ = string.Format("X = {0}, Y = {1}", x, y);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithFormatSpecifier()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var value = 3.14159;
            _ = [|$"Pi = {value:F2}"|];
            """,
            """
            var value = 3.14159;
            _ = string.Format("Pi = {0:F2}", value);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithAlignment()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var name = "Test";
            _ = [|$"Name: {name,10}"|];
            """,
            """
            var name = "Test";
            _ = string.Format("Name: {0,10}", name);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithAlignmentAndFormat()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var value = 123.456;
            _ = [|$"Value: {value,10:F2}"|];
            """,
            """
            var value = 123.456;
            _ = string.Format("Value: {0,10:F2}", value);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithComplexExpression()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var items = new[] { 1, 2, 3 };
            _ = [|$"Count: {items.Length}"|];
            """,
            """
            var items = new[] { 1, 2, 3 };
            _ = string.Format("Count: {0}", items.Length);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithEscapedBraces()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var value = 42;
            _ = [|$"{{value}} = {value}"|];
            """,
            """
            var value = 42;
            _ = string.Format("{{value}} = {0}", value);
            """);
    }

    [Fact]
    public async Task VerbatimInterpolatedString()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var path = "file.txt";
            _ = [|@$"Path: {path}"|];
            """,
            """
            var path = "file.txt";
            _ = string.Format("Path: {0}", path);
            """);
    }

    [Fact]
    public async Task InterpolatedStringWithMultipleFormats()
    {
        await RunRefactoringAsync<ConvertToStringFormatRefactoring>(
            """
            var date = System.DateTime.Now;
            var value = 123.456;
            _ = [|$"Date: {date:yyyy-MM-dd}, Value: {value:C2}"|];
            """,
            """
            var date = System.DateTime.Now;
            var value = 123.456;
            _ = string.Format("Date: {0:yyyy-MM-dd}, Value: {1:C2}", date, value);
            """);
    }
}
