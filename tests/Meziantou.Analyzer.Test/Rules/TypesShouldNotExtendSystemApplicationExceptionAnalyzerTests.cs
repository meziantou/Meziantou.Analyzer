using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

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
        public async Task InheritFromException_ShouldNotReportError()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class Test : System.Exception { }")
                  .ValidateAsync();
        }

        [Fact]
        public async Task InheritFromApplicationException_ShouldReportError()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class [||]Test : System.ApplicationException { }")
                  .ValidateAsync();
        }
    }
}
