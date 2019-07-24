using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_NonAsyncContextTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>(id: "MA0045");
        }

        [TestMethod]
        public async Task PublicNonAsync_Wait_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    public void A()
    {
        Task.Delay(1).Wait();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PublicNonAsync_AsyncSuffix_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    public void A()
    {
        Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PrivateNonAsync_Wait_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [|]Task.Delay(1).Wait();
    }
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task PrivateNonAsync_AsyncSuffix_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [|]Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }
    }
}
