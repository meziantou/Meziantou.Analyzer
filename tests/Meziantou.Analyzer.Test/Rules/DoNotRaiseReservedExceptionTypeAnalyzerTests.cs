using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotRaiseReservedExceptionTypeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotRaiseReservedExceptionTypeAnalyzer>();
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
        throw new ArgumentException();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task RaiseReservedException_ShouldReportErrorAsync()
        {
            const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        throw new IndexOutOfRangeException();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "'System.IndexOutOfRangeException' is a reserved exception type")
                  .ValidateAsync();
        }
    }
}
