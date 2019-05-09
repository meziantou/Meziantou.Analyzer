using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ReturnTaskFromResultInsteadOfReturningNullAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ReturnTaskFromResultInsteadOfReturningNullAnalyzer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task MethodAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    Task A() { return null; }
    Task B() => null;
    Task C() { return ((Test)null)?.A(); }
    async Task<object> Valid() { return null; }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 4, column: 16)
                  .ShouldReportDiagnostic(line: 5, column: 17)
                  .ShouldReportDiagnostic(line: 6, column: 16)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task LocalFunctionAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        Task<object> Valid1() { return Task.FromResult<object>(null); }
        async Task<object> Valid2() { return null; }
        Task A() { return null; }
        Task<object> B() { return null; }
        Task<object> C() => null;
        object       D() => null;
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 8, column: 20)
                  .ShouldReportDiagnostic(line: 9, column: 28)
                  .ShouldReportDiagnostic(line: 10, column: 29)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task LambdaExpressionAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task>         a = () => null;
        System.Func<Task<object>> b = () => null;
        System.Func<Task<object>> c = () => { return null; };
        System.Func<Task>         valid1 = async () => { };
        System.Func<Task<object>> valid2 = async () => null;
        System.Func<object>       valid3 = () => null;
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 45)
                  .ShouldReportDiagnostic(line: 7, column: 45)
                  .ShouldReportDiagnostic(line: 8, column: 47)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task AnonymousMethodsAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task> a = delegate () { return null; };
        System.Func<Task<object>> b = delegate () { return null; };
        System.Func<Task> c = async delegate () { };
        System.Func<Task<object>> d = async delegate () { return null; };
        System.Func<object> e = delegate () { return null; };
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 45)
                  .ShouldReportDiagnostic(line: 7, column: 53)
                  .ValidateAsync();
        }
    }
}
