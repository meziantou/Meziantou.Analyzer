using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotThrowFromFinallyBlockAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotThrowFromFinallyBlockAnalyzer>();
        }

        [Fact]
        public async Task FinallyThrowsDirectly_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        try
        {
        }
        finally
        {
            [|throw new System.Exception(""Unbecoming exception"");|]
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FinallyDoesNotThrow_NoDiagnosticReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        var value = 1;
        try
        {
        }
        finally
        {
            value++;
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FinallyThrowsFromNestedBlock_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        var value = 1;
        try
        {
        }
        finally
        {
            {
                Increment(ref value);
                [|throw new System.Exception($""Unbecoming exception No {value}"");|]
            }
            void Increment(ref int val) => val++;
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FinallyThrowsFromNestedTryCatchBlock_ExceptionIsHandled_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        try
        {
        }
        finally
        {
            try
            {
                [|throw new System.Exception();|]
            }
            catch
            {
            }
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FinallyThrowsFromNestedTryCatchBlock_ExceptionIsUnhandled_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        try
        {
        }
        finally
        {
            try
            {
                [|throw new System.Exception();|]
            }
            catch (System.ArgumentException)
            {
            }
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FinallyThrowsFromSeveralLocations_DiagnosticIsReportedForEachOne()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        try
        {
        }
        finally
        {
            if (true)
            {
                [|throw new System.Exception();|]
            }
            else
            {
                [|throw new System.Exception();|]
            }
        }        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
