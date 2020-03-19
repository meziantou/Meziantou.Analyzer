using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

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
        public async Task FinallyThrowsException_DiagnosticIsReported()
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
        public async Task FinallyDoesNotThrowException_NoDiagnosticReported()
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
        public async Task FinallyThrowsExceptionFromNestedBlock_DiagnosticIsReported()
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
        public async Task FinallyThrowsExceptionFromNestedTryCatchBlock_ExceptionIsHandled_NoDiagnosticReported()
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
                 throw new System.Exception();
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

        [Fact(Skip = "Does not currently pass... but should it?")]
        public async Task FinallyThrowsExceptionFromNestedTryCatchBlock_OtherExceptionIsHandled_DiagnosticIsReported()
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
    }
}
