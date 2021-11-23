using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRaiseNotImplementedExceptionAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotRaiseNotImplementedExceptionAnalyzer>();
    }

    [Fact]
    public async Task RaiseNotReservedException_ShouldNotReportErrorAsync()
    {
        const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        throw new Exception();

        try
        {
        }
        catch (NotImplementedException)
        {
            throw;
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RaiseNotImplementedException_ShouldReportErrorAsync()
    {
        const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        [||]throw new NotImplementedException();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
