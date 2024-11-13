#if CSHARP11_OR_GREATER
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class UseIsPatternInsteadOfSequenceEqualAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseIsPatternInsteadOfSequenceEqualAnalyzer>()
            .WithCodeFixProvider<UseIsPatternInsteadOfSequenceEqualFixer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
            .WithTargetFramework(TargetFramework.Net7_0);
    }

    [Fact]
    public async Task EqualsOrdinal_CSharp10()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
_ = "foo".AsSpan().Equals("bar", StringComparison.Ordinal);
""")
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadOnlySpanByte_SequenceEqual()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
_ = new byte[1].AsSpan().SequenceEqual(new byte[0].AsSpan());
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SpanByte_SequenceEqual()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
Span<byte> value = default;
_ = value.SequenceEqual(new byte[0].AsSpan());
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadOnlySpan_Equals()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;

_ = "foo".AsSpan().Equals("value");
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task EqualsOrdinal_NonConstant()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;

string value = "test";
_ = "foo".AsSpan().Equals(value, StringComparison.Ordinal);
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SequenceEquals_NonConstant()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;

string value = "test";
_ = "foo".AsSpan().SequenceEqual(value);
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SequenceEquals_Comparer()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;

string value = "test";
_ = "foo".AsSpan().SequenceEqual(value, default(System.Collections.Generic.IEqualityComparer<char>));
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadOnlySpanChar_EqualsOrdinal()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
_ = [||]"foo".AsSpan().Equals("bar", StringComparison.Ordinal);
""")
              .ShouldFixCodeWith("""
using System;
_ = "foo".AsSpan() is "bar";
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadOnlySpanChar_EqualsOrdinalIgnoreCase()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
_ = "foo".AsSpan().Equals("bar", StringComparison.OrdinalIgnoreCase);
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task ReadOnlySpanChar_SequenceEqual()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
_ = [||]"foo".AsSpan().SequenceEqual("bar");
""")
              .ShouldFixCodeWith("""
using System;
_ = "foo".AsSpan() is "bar";
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SpanChar_SequenceEqual()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using System;
Span<char> str = default;
_ = [||]str.SequenceEqual("bar");
""")
              .ShouldFixCodeWith("""
using System;
Span<char> str = default;
_ = str is "bar";
""")
              .ValidateAsync();
    }
}
#endif
