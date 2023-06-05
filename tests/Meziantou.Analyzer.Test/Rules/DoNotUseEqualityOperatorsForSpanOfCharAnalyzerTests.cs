using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseEqualityOperatorsForSpanOfCharAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseEqualityOperatorsForSpanOfCharAnalyzer>()
            .WithCodeFixProvider<DoNotUseEqualityOperatorsForSpanOfCharFixer>()
            .WithTargetFramework(TargetFramework.Net5_0);
    }

    [Fact]
    public async Task SpanEquals()
    {
        const string SourceCode = @"
using System;
class Test
{
    void A()
    {
        _ = [|""a"".AsSpan() == ""ab"".AsSpan().Slice(0, 1)|];
    }
}";
        const string CodeFix = @"
using System;
class Test
{
    void A()
    {
        _ = ""a"".AsSpan().SequenceEqual(""ab"".AsSpan().Slice(0, 1));
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task SpanNotEquals()
    {
        const string SourceCode = @"
using System;
class Test
{
    void A()
    {
        _ = [|""a"".AsSpan() != ""ab"".AsSpan().Slice(0, 1)|];
    }
}";
        const string CodeFix = @"
using System;
class Test
{
    void A()
    {
        _ = !""a"".AsSpan().SequenceEqual(""ab"".AsSpan().Slice(0, 1));
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringEquals()
    {
        const string SourceCode = @"
using System;
class Test
{
    void A()
    {
        _ = ""a"" == ""ab"";
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
