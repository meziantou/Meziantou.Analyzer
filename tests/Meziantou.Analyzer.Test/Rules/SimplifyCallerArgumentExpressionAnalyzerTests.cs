using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public class SimplifyCallerArgumentExpressionAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<SimplifyCallerArgumentExpressionAnalyzer>()
            .WithCodeFixProvider<SimplifyCallerArgumentExpressionFixer>()
            .WithTargetFramework(TargetFramework.Net6_0);
    }

    [Fact]
    public async Task ReportDiagnostic()
    {
        const string SourceCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null) { }

    void A(string value)
    {
        NotNull(value.Length, [|""value.Length""|]);
    }
}
";
        const string ExpectedCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null) { }

    void A(string value)
    {
        NotNull(value.Length);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(ExpectedCode)
              .ValidateAsync();
    }
    
    [Fact]
    public async Task ReportDiagnostic_NamedParameter()
    {
        const string SourceCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null, string extra = null) { }

    void A(string value)
    {
        NotNull(value, [|parameterName: ""value""|], ""extra"");
    }
}
";
        const string ExpectedCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null, string extra = null) { }

    void A(string value)
    {
        NotNull(value, ""extra"");
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(ExpectedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NotSameValue()
    {
        const string SourceCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null) { }

    void A(string value)
    {
        NotNull(value, ""value2"");
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValueNotConstant()
    {
        const string SourceCode = @"
using System.Runtime.CompilerServices;
class Sample
{
    void NotNull(object? target, [CallerArgumentExpression(""target"")] string? parameterName = null) { }

    void A(string value)
    {
        NotNull(value, value);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
