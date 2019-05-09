using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class FileNameMustMatchTypeNameAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<FileNameMustMatchTypeNameAnalyzer>();
        }

        [TestMethod]
        public async Task DoesNotMatchFileName()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Sample
{
}")
                  .ShouldReportDiagnostic(line: 2, column: 7)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task DoesMatchFileName()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test0
{
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task DoesMatchFileName_Generic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
class Test0<T>
{
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task DoesMatchFileName_GenericUsingArity()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(fileName: "Test0`1.cs", @"
class Test0<T>
{
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task DoesMatchFileName_GenericUsingOfT()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(fileName: "Test0OfT.cs", @"
class Test0<T>
{
}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task NestedTypeDoesMatchFileName_Ok()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(fileName: "Test0.cs", @"
class Test0
{
    class Test1
    {
    }
}")
                  .ValidateAsync();
        }
    }
}
