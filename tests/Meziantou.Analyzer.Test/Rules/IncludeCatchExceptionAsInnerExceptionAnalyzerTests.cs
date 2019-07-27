using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class IncludeCatchExceptionAsInnerExceptionAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<IncludeCatchExceptionAsInnerExceptionAnalyzer>();
        }

        [Fact]
        public async Task NotInCatchException_ShouldNotReportDiagnostic()
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
        public async Task InCatchExceptionWithInnerException_ShouldNotReportDiagnostic()
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
        public async Task InCatchExceptionWithoutInnerException_ShouldReportDiagnostic()
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
        public async Task InCatchExceptionWithoutInnerException_NoConstructorWithInnerException_ShouldNotReportDiagnostic()
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
