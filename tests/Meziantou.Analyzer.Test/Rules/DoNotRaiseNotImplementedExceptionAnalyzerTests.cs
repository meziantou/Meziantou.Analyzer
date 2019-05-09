using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotRaiseNotImplementedExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotRaiseNotImplementedExceptionAnalyzer>();
        }

        [TestMethod]
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

        [TestMethod]
        public async Task RaiseNotImplementedException_ShouldReportErrorAsync()
        {
            const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        throw new NotImplementedException();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }
    }
}
