using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConditionalCompilationBranchesAreIdenticalAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() =>
        new ProjectBuilder()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithAnalyzer<ConditionalCompilationBranchesAreIdenticalAnalyzer>();

    [Fact]
    public Task IfElif_SameCode() => CreateProjectBuilder()
        .WithSourceCode("""
            #if A
            _ = 0;
            {|MA0202:#elif B|}
            _ = 0;
            #else
            _ = 1;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_SameCode() => CreateProjectBuilder()
        .WithSourceCode("""
            #if A
            _ = 0;
            {|MA0202:#else|}
            _ = 0;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task NonAdjacentDuplicateBranch() => CreateProjectBuilder()
        .WithSourceCode("""
            #if A
            _ = 0;
            #elif B
            _ = 1;
            {|MA0202:#else|}
            _ = 0;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task SameCodeWithDifferentComments() => CreateProjectBuilder()
        .WithSourceCode("""
            #if A
            _ = 0;
            {|MA0202:#elif B|}
            // comment
            _ = 0;
            #else
            _ = 1;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task DifferentBranches() => CreateProjectBuilder()
        .WithSourceCode("""
            #if A
            _ = 0;
            #elif B
            _ = 1;
            #else
            _ = 2;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_SameCode_PartialExpression() => CreateProjectBuilder()
        .WithSourceCode("""
            _ =
            #if A
             1;
            {|MA0202:#else|}
             1;
            #endif
            """)
        .ValidateAsync();

    [Fact]
    public Task IfElse_SameCode_PartialTypeDeclaration() => CreateProjectBuilder()
        .WithSourceCode("""
            interface ISample { }
            interface ISpanFormattable { }

            #if A
             public
            #else
             internal
            #endif
            class Sample : ISample
            #if NET10_0
            , ISpanFormattable
            {|MA0202:#else|}
            , ISpanFormattable
            #endif
            { }

            static class Program { static void Main() { } }
            """)
        .ValidateAsync();
}
