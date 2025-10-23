#pragma warning disable CA1030 // Use events where appropriate
using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRaiseApplicationExceptionAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotRaiseApplicationExceptionAnalyzer>();
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
        throw new ArgumentException();

        try
        {
        }
        catch (ApplicationException)
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
    public async Task RaiseReservedException_ShouldReportErrorAsync()
    {
        const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        [||]throw new ApplicationException();
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
