using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseReturnTagForVoidMethodAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() =>
        new ProjectBuilder()
            .WithAnalyzer<DoNotUseReturnTagForVoidMethodAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);

    [Theory]
    [InlineData("returns")]
    [InlineData("return")]
    public Task VoidMethod_ReturnTag(string tag) => CreateProjectBuilder()
        .WithSourceCode($$"""
            class Sample
            {
                /// <summary>Does something.</summary>
                /// {|MA0203:<{{tag}}>|}The result.</{{tag}}>
                void M()
                {
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task VoidMethod_EmptyReturnTag() => CreateProjectBuilder()
        .WithSourceCode("""
            class Sample
            {
                /// <summary>Does something.</summary>
                /// {|MA0203:<returns />|}
                void M()
                {
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task NonVoidMethod_ReturnTag() => CreateProjectBuilder()
        .WithSourceCode("""
            class Sample
            {
                /// <summary>Gets a value.</summary>
                /// <returns>The result.</returns>
                int M() => 0;
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task VoidMethod_NoReturnTag() => CreateProjectBuilder()
        .WithSourceCode("""
            class Sample
            {
                /// <summary>Does something.</summary>
                void M()
                {
                }
            }
            """)
        .ValidateAsync();

    [Fact]
    public Task Constructor_ReturnTag() => CreateProjectBuilder()
        .WithSourceCode("""
            class Sample
            {
                /// <summary>Initializes a new instance.</summary>
                /// <returns>The result.</returns>
                public Sample()
                {
                }
            }
            """)
        .ValidateAsync();
}
