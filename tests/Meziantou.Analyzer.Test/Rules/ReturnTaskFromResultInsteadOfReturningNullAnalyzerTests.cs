using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ReturnTaskFromResultInsteadOfReturningNullAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ReturnTaskFromResultInsteadOfReturningNullAnalyzer>()
            .WithCodeFixProvider<ReturnTaskFromResultInsteadOfReturningNullFixer>();
    }

    [Fact]
    public async Task MethodFixer_Task_Completed()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    Task A() { [||]return null; }
}
""";
        var fix = """
using System.Threading.Tasks;
class Test
{
    Task A() { return Task.CompletedTask; }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodFixer_Task_Completed2()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    Task A() => [||]null;
}
""";
        var fix = """
using System.Threading.Tasks;
class Test
{
    Task A() => Task.CompletedTask;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodFixer_Task_FromResult()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    Task<int> A() { [||]return null; }
}
""";
        var fix = """
using System.Threading.Tasks;
class Test
{
    Task<int> A() { return Task.FromResult(0); }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodFixer_Task_FromResult2()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    Task<string> A() => [||]null;
}
""";
        var fix = """
using System.Threading.Tasks;
class Test
{
    Task<string> A() => Task.FromResult<string>(null);
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Task A() { [||]return null; }")]
    [InlineData("Task A() { [||]return default; }")]
    [InlineData("Task A() => [||]null;")]
    [InlineData("Task A() => [||]default;")]
    [InlineData("Task A() { [||]return ((Test)null)?.A(); }")]
    [InlineData("Task A() { [||]return 1 switch { _ => null }; }")]
    [InlineData("Task A(int value) { [||]return value switch { 1 => A(0), _ => null }; }")]
    [InlineData("Task A(bool a) { [||]return a ? null : A(a); }")]
    [InlineData("Task<object> A() { [||]return null; }")]
    [InlineData("Task<object> A() { [||]return default; }")]
    [InlineData("Task<object> A() => [||]null;")]
    [InlineData("Task<object> A() => [||]default;")]
    [InlineData("async Task<object> Valid() { return null; }")]
    [InlineData("object Valid() { return null; }")]
    public async Task Method(string code)
    {
        var sourceCode = $$"""
using System.Threading.Tasks;
class Test
{
    {{code}}
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LocalFunction()
    {
        const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        Task<object> Valid1() { return Task.FromResult<object>(null); }
        async Task<object> Valid2() { return null; }
        Task A() { [||]return null; }
        Task<object> B() { [||]return null; }
        Task<object> C() => [||]null;
        object       D() => null;
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LambdaExpression()
    {
        const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task>         a = () => [||]null;
        System.Func<Task<object>> b = () => [||]null;
        System.Func<Task<object>> c = () => { [||]return null; };
        System.Func<Task>         valid1 = async () => { };
        System.Func<Task<object>> valid2 = async () => null;
        System.Func<object>       valid3 = () => null;
        System.Func<Task<object>> valid4 = async () => { return null; };
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AnonymousMethods()
    {
        const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task> a = delegate () { [||]return null; };
        System.Func<Task<object>> b = delegate () { [||]return null; };
        System.Func<Task> c = async delegate () { };
        System.Func<Task<object>> d = async delegate () { return null; };
        System.Func<object> e = delegate () { return null; };
        System.Action f = () => { };
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task AsyncLambdaInTask()
    {
        const string SourceCode = @"using System.Threading.Tasks;
class Test
{
    Task A()
    {   
        System.Func<Task<object>> valid4 = async () => { return null; };
        return Task.CompletedTask;
    }
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
