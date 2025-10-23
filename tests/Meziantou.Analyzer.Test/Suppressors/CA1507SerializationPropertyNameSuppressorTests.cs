#if ROSLYN_4_10_OR_GREATER
using Meziantou.Analyzer.Suppressors;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Suppressors;
public sealed class CA1507SerializationPropertyNameSuppressorTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net9_0)
            .WithMicrosoftCodeAnalysisNetAnalyzers("CA1507")
            .WithAnalyzer<CA1507SerializationPropertyNameSuppressor>();
    }

    [Fact]
    public async Task CA1507IsReported()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                internal class Test
                {
                    public void Foo(string name) => throw new System.ArgumentException("dummy", [|"name"|]);
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1507_STJ_JsonPropertyName()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                internal class Test
                {
                    [System.Text.Json.Serialization.JsonPropertyName("Foo")]
                    public int Foo { get; set; }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1507_NewtonsoftJson_JsonPropertyName()
    {
        await CreateProjectBuilder()
            .AddNuGetReference("Newtonsoft.Json", "13.0.3", "lib/netstandard2.0/")
            .WithSourceCode("""
                internal class Test
                {
                    [Newtonsoft.Json.JsonProperty("Foo")]
                    public int Foo { get; set; }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task CA1507_NewtonsoftJson_JsonPropertyName_NamedParameter()
    {
        await CreateProjectBuilder()
            .AddNuGetReference("Newtonsoft.Json", "13.0.3", "lib/netstandard2.0/")
            .WithSourceCode("""
                internal class Test
                {
                    [Newtonsoft.Json.JsonProperty(propertyName: "Foo")]
                    public int Foo { get; set; }
                }
                """)
            .ValidateAsync();
    }
}
#endif