using Meziantou.Analyzer.Refactorings;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Meziantou.Analyzer.Test.Refactorings;

public sealed class MakeInterpolatedStringRefactoringTests
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
    public async Task SimpleString()
    {
        await RunRefactoringAsync<MakeInterpolatedStringRefactoring>(
            """_ = [|"test"|];""",
            """_ = $"test";""");
    }

    [Fact]
    public async Task VerbatimString()
    {
        await RunRefactoringAsync<MakeInterpolatedStringRefactoring>(
            """_ = [|@"test"|];""",
            """_ = $@"test";""");
    }

    [Fact]
    public async Task SimpleStringWithOpenAndCloseCurlyBraces()
    {
        await RunRefactoringAsync<MakeInterpolatedStringRefactoring>(
            """_ = [|"test{0}"|];""",
            """_ = $"test{0}";""");
    }

    [Fact]
    public async Task VerbatimStringWithOpenAndCloseCurlyBraces()
    {
        await RunRefactoringAsync<MakeInterpolatedStringRefactoring>(
            """_ = [|@"test{0}"|];""",
            """_ = $@"test{0}";""");
    }
}
