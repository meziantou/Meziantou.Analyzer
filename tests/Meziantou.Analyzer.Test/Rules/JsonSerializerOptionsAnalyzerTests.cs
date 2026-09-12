using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.JsonSerializerOptionsAnalyzer>;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.JsonSerializerOptionsAnalyzer,
    Meziantou.Analyzer.Rules.JsonSerializerOptionsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class JsonSerializerOptionsAnalyzerTests
{
    [Theory]
    [InlineData("RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true")]
    [InlineData("RespectNullableAnnotations = false, RespectRequiredConstructorParameters = false")]
    [InlineData("RespectNullableAnnotations = true, RespectRequiredConstructorParameters = false")]
    public Task BothOptionsSet(string options)
    {
        var test = new AnalyzerTest();
        test.TestCode = $$"""
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new() { {{options}} };
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("RespectRequiredConstructorParameters = true")]
    [InlineData("RespectRequiredConstructorParameters = false")]
    public Task MissingRespectNullableAnnotations(string options)
    {
        var test = new AnalyzerTest();
        test.TestCode = $$"""
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:new()|} { {{options}} };
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("RespectNullableAnnotations = true")]
    [InlineData("RespectNullableAnnotations = false")]
    public Task MissingRespectRequiredConstructorParameters(string options)
    {
        var test = new AnalyzerTest();
        test.TestCode = $$"""
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0225:new()|} { {{options}} };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreationWithoutTheOptions()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreationWithoutTheOptions_ReportsTheCreationWithoutItsInitializer()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:{|MA0225:new JsonSerializerOptions|}|}
                {
                    WriteIndented = true,
                };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocal()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A()
                {
                    var options = new JsonSerializerOptions();
                    options.RespectNullableAnnotations = true;
                    options.RespectRequiredConstructorParameters = false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalAssignedAfterItsDeclaration()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A()
                {
                    JsonSerializerOptions options;
                    options = new JsonSerializerOptions();
                    options.RespectNullableAnnotations = true;
                    options.RespectRequiredConstructorParameters = true;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalBeforeItIsAssignedANewInstance()
    {
        // The first statement configures the first instance, not the one created afterwards
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A()
                {
                    var options = {|MA0225:new JsonSerializerOptions()|};
                    options.RespectNullableAnnotations = true;
                    options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalAfterItIsAssignedANewInstance()
    {
        // The last statements configure the last instance, not the one created before
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A()
                {
                    var options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
                    options = new JsonSerializerOptions();
                    options.RespectNullableAnnotations = true;
                    options.RespectRequiredConstructorParameters = true;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalInAnEnclosingBlock()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A(bool condition)
                {
                    JsonSerializerOptions options;
                    if (condition)
                    {
                        options = new JsonSerializerOptions();
                    }
                    else
                    {
                        options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                    }

                    options.RespectNullableAnnotations = true;
                    options.RespectRequiredConstructorParameters = true;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalInTheSameSwitchSection()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A(int value)
                {
                    JsonSerializerOptions options;
                    switch (value)
                    {
                        case 0:
                            options = new JsonSerializerOptions();
                            options.RespectNullableAnnotations = true;
                            options.RespectRequiredConstructorParameters = true;
                            break;

                        default:
                            options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
                            break;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnAnotherLocal()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                void A(JsonSerializerOptions other)
                {
                    var options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
                    var configured = new JsonSerializerOptions { RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true };
                    other.RespectNullableAnnotations = true;
                    other.RespectRequiredConstructorParameters = true;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionsSetOnTheLocalInAnotherMethod()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                JsonSerializerOptions A() => {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};

                void B()
                {
                    var options = A();
                    options.RespectNullableAnnotations = true;
                    options.RespectRequiredConstructorParameters = true;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CopyConstructor()
    {
        // The copied instance can be configured somewhere else
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                JsonSerializerOptions A(JsonSerializerOptions other) => new JsonSerializerOptions(other);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StrictDefaults()
    {
        // JsonSerializerDefaults.Strict, introduced in .NET 10, sets both options
        var test = new AnalyzerTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Strict) { RespectNullableAnnotations = false };
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("JsonSerializerDefaults.Web")]
    [InlineData("JsonSerializerDefaults.General")]
    public Task DefaultsNotSettingTheOptions(string arguments)
    {
        var test = new AnalyzerTest();
        test.TestCode = $$"""
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:{|MA0225:new JsonSerializerOptions({{arguments}})|}|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OtherType()
    {
        var test = new AnalyzerTest();
        test.TestCode = """
            class Sample
            {
                static readonly Sample Instance = new();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnsupportedTargetFramework()
    {
        // The options were introduced in .NET 9
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsRespectNullableAnnotations()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:new()|} { RespectRequiredConstructorParameters = true };
            }
            """;
        test.FixedCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new() { RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsRespectRequiredConstructorParameters()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0225:new()|} { RespectNullableAnnotations = false };
            }
            """;
        test.FixedCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new() { RespectNullableAnnotations = false, RespectRequiredConstructorParameters = true };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsTheInitializer()
    {
        var test = new CodeFixTest();

        // Both fixes change the same creation, so they cannot be batched in a single iteration
        test.NumberOfFixAllIterations = 2;
        test.TestCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = {|MA0224:{|MA0225:new JsonSerializerOptions()|}|};
            }
            """;
        test.FixedCode = """
            using System.Text.Json;

            class Sample
            {
                static readonly JsonSerializerOptions Options = new JsonSerializerOptions() { RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true };
            }
            """;

        return test.RunAsync();
    }
}
