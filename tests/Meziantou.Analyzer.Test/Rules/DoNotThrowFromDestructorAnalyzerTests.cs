using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotThrowFromDestructorAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotThrowFromDestructorAnalyzer>();
        }

        [Fact]
        public async Task Destructor_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    ~TestClass()
    {
        [|throw new System.Exception(""Unbecoming exception"");|]        
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task DestructorDoesNotThrow_NoDiagnosticReported()
        {
            const string SourceCode = @"
class TestClass
{
    ~TestClass()
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
        public async Task DestructorThrowsFromNestedBlock_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    ~TestClass()
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
        public async Task DestructorThrowsFromNestedTryCatchBlock_ExceptionIsHandled_DiagnosticIsReported()
        {
            const string SourceCode = @"
class TestClass
{
    ~TestClass()
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
    }
}
