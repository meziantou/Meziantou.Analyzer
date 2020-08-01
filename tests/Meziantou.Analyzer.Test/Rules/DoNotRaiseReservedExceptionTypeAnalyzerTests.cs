using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotRaiseReservedExceptionTypeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotRaiseReservedExceptionTypeAnalyzer>();
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
        [||]throw new IndexOutOfRangeException();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnosticWithMessage("'System.IndexOutOfRangeException' is a reserved exception type")
                  .ValidateAsync();
        }

        [Fact]
        public async Task ThrowNull()
        {
            const string SourceCode = @"using System;
class TestAttribute
{
    void Test()
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
