using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer>()
            .WithCodeFixProvider<DoNotRemoveOriginalExceptionFromThrowStatementFixer>();
    }

    [Fact]
    public async Task NoDiagnostic()
    {
        const string SourceCode = @"
class Test
{
    internal void Sample()
    {
        throw new System.Exception();

        try
        {
            throw new System.Exception();
        }
        catch (System.Exception ex)
        {
            throw new System.Exception(""test"", ex);
        }
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldReportDiagnostic_DerivedException()
    {
        const string SourceCode = """
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.InvalidOperationException ex)
        {
            [||]throw ex;
        }
    }
}
""";
        const string CodeFix = """
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.InvalidOperationException ex)
        {
            throw;
        }
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ShouldReportDiagnostic()
    {
        const string SourceCode = @"
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            _ = ex;
            [||]throw ex;
        }
    }
}
";
        const string CodeFix = @"
class Test
{
    internal void Sample()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            _ = ex;
            throw;
        }
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }
}
