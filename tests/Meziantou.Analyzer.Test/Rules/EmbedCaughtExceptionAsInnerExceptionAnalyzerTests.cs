using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class EmbedCaughtExceptionAsInnerExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<EmbedCaughtExceptionAsInnerExceptionAnalyzer>();
        }

        [Fact]
        public async Task NotInCaughtException_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        throw new System.Exception("""");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task InCaughtExceptionWithInnerException_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            throw new System.Exception("""", ex);
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task InCaughtExceptionWithoutInnerException_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            throw [||]new System.Exception("""");
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task InCaughtExceptionWithoutInnerException_NoConstructorWithInnerException_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public void A()
    {
        try
        {
        }
        catch (System.Exception ex)
        {
            throw new CustomException("""");
        }
    }
}

class CustomException : System.Exception
{
    public CustomException(string message)
    {
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
