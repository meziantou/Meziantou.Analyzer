using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class TypesShouldNotExtendSystemApplicationExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<TypesShouldNotExtendSystemApplicationExceptionAnalyzer>();
        }

        [Fact]
        public async System.Threading.Tasks.Task InheritFromException_ShouldNotReportErrorAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class Test : System.Exception { }")
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task InheritFromApplicationException_ShouldReportErrorAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class [||]Test : System.ApplicationException { }")
                  .ValidateAsync();
        }
    }
}
