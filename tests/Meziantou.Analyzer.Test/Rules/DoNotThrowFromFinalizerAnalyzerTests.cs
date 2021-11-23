using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotThrowFromFinalizerAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotThrowFromFinalizerAnalyzer>();
    }

    [Fact]
    public async Task Finalizer_DiagnosticIsReported()
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
    public async Task FinalizerDoesNotThrow_NoDiagnosticReported()
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
    public async Task FinalizerThrowsFromNestedBlock_DiagnosticIsReported()
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
    public async Task FinalizerThrowsFromNestedTryCatchBlock_ExceptionIsHandled_DiagnosticIsReported()
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
