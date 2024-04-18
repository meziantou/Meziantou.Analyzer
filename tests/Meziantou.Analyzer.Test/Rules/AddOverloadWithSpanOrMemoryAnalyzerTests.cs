using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AddOverloadWithSpanOrMemoryAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net6_0)
            .WithAnalyzer<AddOverloadWithSpanOrMemoryAnalyzer>();
    }

    [Fact]
    public async Task StringArrayWithoutSpanOverload_Params()
    {
        const string SourceCode = @"
public class Test
{
    public void A(params string[] a)
    {
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArrayWithoutSpanOverload_Out()
    {
        const string SourceCode = @"
public class Test
{
    public void A(out byte[] a) => throw null;
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArrayWithSpanOverload_Params()
    {
        const string SourceCode = @"
public class Test
{
    public void A(params string[] a) { }
    public void A(System.ReadOnlySpan<string> a) { }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArrayWithoutSpanOverload()
    {
        const string SourceCode = @"
public class Test
{
    public void [||]A(string[] a)
    {
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringArrayWithoutSpanOverload_Complex()
    {
        const string SourceCode = @"
public class Test
{
    public void [||]A(string[] a, int b) { }
    public void A(System.ReadOnlySpan<string> a, string b) { } // Not same type for b
    public void A(System.ReadOnlySpan<string> a, int b, int c) { } // not same number of parameters
    public void A(System.ReadOnlySpan<string> a, System.ReadOnlySpan<int> b) { } // Not same type for b
    public void B(System.ReadOnlySpan<string> a, int b) { } // Not same method name
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Span<string>")]
    [InlineData("System.ReadOnlySpan<string>")]
    [InlineData("System.Memory<string>")]
    [InlineData("System.ReadOnlyMemory<string>")]
    public async Task StringArrayWithSpanOverload(string overloadType)
    {
        var sourceCode = @"
public class Test
{
    public void A(string[] a) { }
    public void A(" + overloadType + @" a) { }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Span<char>")]
    [InlineData("System.ReadOnlySpan<char>")]
    [InlineData("System.Memory<char>")]
    [InlineData("System.ReadOnlyMemory<char>")]
    public async Task StringArrayWithSpanOverload_String(string overloadType)
    {
        var sourceCode = @"
public class Test
{
    public void A(string a) { }
    public void A(" + overloadType + @" a) { }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("System.Span<string>")]
    [InlineData("System.ReadOnlySpan<string>")]
    [InlineData("System.Memory<string>")]
    [InlineData("System.ReadOnlyMemory<string>")]
    public async Task SpanWithoutOverload(string overloadType)
    {
        var sourceCode = @"
public class Test
{
    public void A(" + overloadType + @" a) { }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}
