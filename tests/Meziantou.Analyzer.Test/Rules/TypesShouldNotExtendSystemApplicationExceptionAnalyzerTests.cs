using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class TypesShouldNotExtendSystemApplicationExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<TypesShouldNotExtendSystemApplicationExceptionAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task InheritFromException_ShouldNotReportErrorAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class Test : System.Exception { }")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task InheritFromApplicationException_ShouldReportErrorAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode("class Test : System.ApplicationException { }")
                  .ShouldReportDiagnostic(line: 1, column: 7)
                  .ValidateAsync();
        }
    }
}
