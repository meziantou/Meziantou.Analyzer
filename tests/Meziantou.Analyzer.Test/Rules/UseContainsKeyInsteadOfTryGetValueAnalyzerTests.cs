using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class UseContainsKeyInsteadOfTryGetValueAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseContainsKeyInsteadOfTryGetValueAnalyzer>();
    }

    [Fact]
    public async Task IDictionary_TryGetValue_Value()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class ClassTest
                {
                    void Test(System.Collections.Generic.IDictionary<string, string> dict)
                    {
                        dict.TryGetValue("", out var a);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IDictionary_TryGetValue_Discard()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class ClassTest
                {
                    void Test(System.Collections.Generic.IDictionary<string, string> dict)
                    {
                        [||]dict.TryGetValue("", out _);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task IReadOnlyDictionary_TryGetValue_Discard()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class ClassTest
                {
                    void Test(System.Collections.Generic.IReadOnlyDictionary<string, string> dict)
                    {
                        [||]dict.TryGetValue("", out _);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_TryGetValue_Discard()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class ClassTest
                {
                    void Test(System.Collections.Generic.Dictionary<string, string> dict)
                    {
                        [||]dict.TryGetValue("", out _);
                    }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CustomDictionary_TryGetValue_Discard()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class ClassTest
                {
                    void Test(SampleDictionary dict)
                    {
                        [||]dict.TryGetValue("", out _);
                    }
                }

                class SampleDictionary : System.Collections.Generic.Dictionary<string, string>
                {
                }
                """)
              .ValidateAsync();
    }
}
