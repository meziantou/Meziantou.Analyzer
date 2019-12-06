using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class NonConstantStaticFieldsShouldNotBeVisibleAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<NonConstantStaticFieldsShouldNotBeVisibleAnalyzer>();
        }

        [Fact]
        public async Task ReportDiagnostic()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
public class Sample
{
    public static int [||]a = 0;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task InternalClass()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
internal class Sample
{
    public static int a = 0;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task StaticReadOnly()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
public class Sample
{
    public static readonly int a = 0;
}")
                  .ValidateAsync();
        }

        [Fact]
        public async Task InstanceField()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
public class Sample
{
    public int a = 0;
}")
                  .ValidateAsync();
        }
    }
}
