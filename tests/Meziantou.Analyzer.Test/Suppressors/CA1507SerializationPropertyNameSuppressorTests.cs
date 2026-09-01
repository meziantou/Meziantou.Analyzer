#if ROSLYN_4_10_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Suppressors.CA1507SerializationPropertyNameSuppressor>;

namespace Meziantou.Analyzer.Test.Suppressors;

public sealed class CA1507SerializationPropertyNameSuppressorTests
{
    /// <summary>
    /// The diagnostic CA1507 reports on the string literal marked with <c>{|#N:code|}</c>,
    /// which the suppressor is expected to suppress or not.
    /// </summary>
    private static DiagnosticResult CA1507(int location, bool suppressed) =>
        new DiagnosticResult("CA1507", DiagnosticSeverity.Info).WithLocation(location).WithIsSuppressed(suppressed);

    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net90;
        test.AddMicrosoftCodeAnalysisNetAnalyzers("CA1507");
        return test;
    }

    [Fact]
    public Task CA1507IsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Test
            {
                public void Foo(string name) => throw new System.ArgumentException("dummy", {|#0:"name"|});
            }
            """;
        test.ExpectedDiagnostics.Add(CA1507(0, suppressed: false));

        return test.RunAsync();
    }

    [Fact]
    public Task CA1507_STJ_JsonPropertyName()
    {
        var test = CreateTest();
        // Microsoft.CodeAnalysis.NetAnalyzers does not report CA1507 on the arguments of this attribute anymore,
        // so there is nothing left for the suppressor to suppress. The test still guards against it reporting again.
        test.TestCode = """
            internal class Test
            {
                [System.Text.Json.Serialization.JsonPropertyName("Foo")]
                public int Foo { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CA1507_NewtonsoftJson_JsonPropertyName()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNewtonsoftJson();
        test.TestCode = """
            internal class Test
            {
                [Newtonsoft.Json.JsonProperty({|#0:"Foo"|})]
                public int Foo { get; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(CA1507(0, suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task CA1507_NewtonsoftJson_JsonPropertyName_NamedParameter()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNewtonsoftJson();
        test.TestCode = """
            internal class Test
            {
                [Newtonsoft.Json.JsonProperty(propertyName: {|#0:"Foo"|})]
                public int Foo { get; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(CA1507(0, suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task CA1507_NewtonsoftJson_JsonPropertyName_AfterAnUnrelatedDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNewtonsoftJson();
        test.TestCode = """
            internal class Test
            {
                public void Foo(string name) => throw new System.ArgumentException("dummy", {|#0:"name"|});

                [Newtonsoft.Json.JsonProperty({|#1:"Bar"|})]
                public int Bar { get; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(CA1507(0, suppressed: false));
        test.ExpectedDiagnostics.Add(CA1507(1, suppressed: true));

        return test.RunAsync();
    }
}
#endif
