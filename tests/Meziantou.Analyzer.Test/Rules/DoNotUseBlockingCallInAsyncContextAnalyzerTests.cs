using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseBlockingCallInAsyncContextAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>();
        }

        [TestMethod]
        public async Task NonAsync_Wait_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .ShouldNotReportDiagnostic()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public void A()
    {
        Task.Delay(1).Wait();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Async_Wait_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Task.Delay(1).Wait();
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Async_Result_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        _ = Task.FromResult(1).Result;
    }
}")
                  .ShouldReportDiagnostic(line: 6, column: 13)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Async_SuggestOverload_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task Write(int a) => throw null;
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NonAsync_AsyncSuffix_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public void A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Async_AsyncSuffix_Diagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ShouldReportDiagnostic(line: 6, column: 9)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Async_NoOverload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public void WriteAsync() => throw null;
}")
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AsyncLambda_Overload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        System.Func<Task> a = async () => Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ShouldReportDiagnostic(line: 6, column: 43)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task AsyncLocalFunction_Overload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public void A()
    {
        Local();

        async Task Local() => Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ShouldReportDiagnostic(line: 8, column: 31)
                  .ValidateAsync();
        }


        [TestMethod]
        public async Task Method_NoOverload_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
class Test
{
    public async Task A()
    {
        Write();
    }

    public void Write() => throw null;
    public void Write(int a) => throw null;
}")
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }
    }
}
