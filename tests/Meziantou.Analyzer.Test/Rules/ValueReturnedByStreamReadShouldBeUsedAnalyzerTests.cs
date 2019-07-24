using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class ValueReturnedByStreamReadShouldBeUsedAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ValueReturnedByStreamReadShouldBeUsedAnalyzer>();
        }

        [TestMethod]
        public async Task Read_ReturnValueNotUsed()
        {
            const string SourceCode = @"using System.IO;
class Test
{
    void A()
    {
        var stream = File.OpenRead("""");
        [||]stream.Read(null, 0, 0);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ReadAsync_ReturnValueNotUsed()
        {
            const string SourceCode = @"using System.IO;
class Test
{
    async void A()
    {
        var stream = File.OpenRead("""");
        await [||]stream.ReadAsync(null, 0, 0);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ReadAsync_ReturnValueUsed_DiscardOperator()
        {
            const string SourceCode = @"using System.IO;
class Test
{
    async void A()
    {
        var stream = File.OpenRead("""");
        _ = await stream.ReadAsync(null, 0, 0);
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Read_ReturnValueUsed_MethodCall()
        {
            const string SourceCode = @"using System.IO;
class Test
{
    async void A()
    {
        var stream = File.OpenRead("""");
        System.Console.Write(stream.Read(null, 0, 0));
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
