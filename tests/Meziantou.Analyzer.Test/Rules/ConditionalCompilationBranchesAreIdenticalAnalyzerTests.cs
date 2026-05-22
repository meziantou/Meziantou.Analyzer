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
}
