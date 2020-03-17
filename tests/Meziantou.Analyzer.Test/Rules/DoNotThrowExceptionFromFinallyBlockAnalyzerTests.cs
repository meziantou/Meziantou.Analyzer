using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotThrowExceptionFromFinallyBlockAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotThrowExceptionFromFinallyBlockAnalyzer>();
        }

        [Fact]
        public async Task ThrowExceptionFromFinallyBlock_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        try
        {
            throw new System.Exception(""Triggering exception"");
        }
        finally
        {
            [|throw new System.Exception(""Unbecoming exception"");|]
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task DoNotThrowExceptionFromFinallyBlock_NoDiagnosticReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        var value = 1;
        try
        {
            throw new System.Exception(""Triggering exception"");
        }
        finally
        {
            value++;
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
