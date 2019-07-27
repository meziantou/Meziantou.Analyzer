using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotUseBlockingCallInAsyncContextAnalyzer_NonAsyncContextTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotUseBlockingCallInAsyncContextAnalyzer>(id: "MA0045");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public async Task PrivateNonAsync_Wait_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [||]Task.Delay(1).Wait();
    }
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task PrivateNonAsync_AsyncSuffix_NoDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"using System.Threading.Tasks;
public class Test
{
    private void A()
    {
        [||]Write();
    }

    public void Write() => throw null;
    public Task WriteAsync() => throw null;
}")
                  .ValidateAsync();
        }
    }
}
