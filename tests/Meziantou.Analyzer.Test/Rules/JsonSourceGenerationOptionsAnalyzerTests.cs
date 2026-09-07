using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.JsonSourceGenerationOptionsAnalyzer>;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.JsonSourceGenerationOptionsAnalyzer,
    Meziantou.Analyzer.Rules.JsonSourceGenerationOptionsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class JsonSourceGenerationOptionsAnalyzerTests
{
    /// <summary>
    /// The target framework the tests compile against, or <see langword="null"/> to use the latest one the harness
    /// provides. The source generators shipped with a .NET version cannot be loaded by the older versions of
    /// Roslyn, so the tests lower the target framework to the latest one the running version of Roslyn can load.
    /// </summary>
    private static ReferenceAssemblies? DefaultReferenceAssemblies =>
#if ROSLYN_5_0_OR_GREATER
        null;
#elif ROSLYN_4_14_OR_GREATER
        ReferenceAssemblies.Net.Net100;
#else
        ReferenceAssemblies.Net.Net90;
#endif

    // The System.Text.Json source generator implements the members of JsonSerializerContext the tests do not declare
    private static AnalyzerTest CreateTest(ReferenceAssemblies? referenceAssemblies = null)
    {
        var test = new AnalyzerTest { UseFrameworkSourceGenerators = true };
        if ((referenceAssemblies ?? DefaultReferenceAssemblies) is { } assemblies)
        {
            test.ReferenceAssemblies = assemblies;
        }

        return test;
    }

    private static CodeFixTest CreateCodeFixTest()
    {
        var test = new CodeFixTest { UseFrameworkSourceGenerators = true };
        if (DefaultReferenceAssemblies is { } assemblies)
        {
            test.ReferenceAssemblies = assemblies;
        }

        return test;
    }

    [Theory]
    [InlineData("RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true")]
    [InlineData("RespectNullableAnnotations = false, RespectRequiredConstructorParameters = false")]
    [InlineData("RespectNullableAnnotations = true, RespectRequiredConstructorParameters = false")]
    public Task BothOptionsSet(string options)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.Json.Serialization;

            [JsonSourceGenerationOptions({{options}})]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("RespectRequiredConstructorParameters = true")]
    [InlineData("RespectRequiredConstructorParameters = false")]
    public Task MissingRespectNullableAnnotations(string options)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.Json.Serialization;

            [{|MA0222:JsonSourceGenerationOptions({{options}})|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("RespectNullableAnnotations = true")]
    [InlineData("RespectNullableAnnotations = false")]
    public Task MissingRespectRequiredConstructorParameters(string options)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.Json.Serialization;

            [{|MA0223:JsonSourceGenerationOptions({{options}})|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AttributeWithoutTheOptions()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.Json.Serialization;

            [{|MA0222:{|MA0223:JsonSourceGenerationOptions(WriteIndented = true)|}|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MissingAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text.Json.Serialization;

            [JsonSerializable(typeof(int))]
            partial class {|MA0222:{|MA0223:SampleContext|}|} : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

#if ROSLYN_4_14_OR_GREATER // The .NET 10 source generators cannot be loaded by the older versions of Roslyn
    [Theory]
    [InlineData("JsonSerializerDefaults.Strict")]
    [InlineData("JsonSerializerDefaults.Strict, RespectNullableAnnotations = false")]
    public Task StrictDefaults(string arguments)
    {
        // JsonSerializerDefaults.Strict, introduced in .NET 10, sets both options
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.Json;
            using System.Text.Json.Serialization;

            [JsonSourceGenerationOptions({{arguments}})]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("JsonSerializerDefaults.Web")]
    [InlineData("JsonSerializerDefaults.General")]
    public Task DefaultsNotSettingTheOptions(string arguments)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text.Json;
            using System.Text.Json.Serialization;

            [{|MA0222:{|MA0223:JsonSourceGenerationOptions({{arguments}})|}|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }
#endif

    [Fact]
    public Task Fix_AddsRespectNullableAnnotations()
    {
        var test = CreateCodeFixTest();
        test.TestCode = """
            using System.Text.Json.Serialization;

            [{|MA0222:JsonSourceGenerationOptions(RespectRequiredConstructorParameters = true)|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;
        test.FixedCode = """
            using System.Text.Json.Serialization;

            [JsonSourceGenerationOptions(RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true)]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsRespectRequiredConstructorParameters()
    {
        var test = CreateCodeFixTest();
        test.TestCode = """
            using System.Text.Json.Serialization;

            [{|MA0223:JsonSourceGenerationOptions(RespectNullableAnnotations = false)|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;
        test.FixedCode = """
            using System.Text.Json.Serialization;

            [JsonSourceGenerationOptions(RespectNullableAnnotations = false, RespectRequiredConstructorParameters = true)]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsBothOptions()
    {
        var test = CreateCodeFixTest();

        // Both fixes change the same attribute, so they cannot be batched in a single iteration
        test.NumberOfFixAllIterations = 2;
        test.TestCode = """
            using System.Text.Json.Serialization;

            [{|MA0222:{|MA0223:JsonSourceGenerationOptions(WriteIndented = true)|}|}]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;
        test.FixedCode = """
            using System.Text.Json.Serialization;

            [JsonSourceGenerationOptions(WriteIndented = true, RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true)]
            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_AddsTheAttribute()
    {
        var test = CreateCodeFixTest();

        // The first fix adds the attribute, the second one adds the remaining option to it
        test.NumberOfFixAllIterations = 2;
        test.TestCode = """
            using System.Text.Json.Serialization;

            [JsonSerializable(typeof(int))]
            partial class {|MA0222:{|MA0223:SampleContext|}|} : JsonSerializerContext;
            """;
        test.FixedCode = """
            using System.Text.Json.Serialization;

            [JsonSerializable(typeof(int))]
            [JsonSourceGenerationOptions(RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true)]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAJsonSerializerContext()
    {
        var test = CreateTest();
        test.TestCode = """
            class SampleContext;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TargetFrameworkWithoutTheOptions()
    {
        var test = CreateTest(ReferenceAssemblies.Net.Net80);
        test.TestCode = """
            using System.Text.Json.Serialization;

            [JsonSerializable(typeof(int))]
            partial class SampleContext : JsonSerializerContext;
            """;

        return test.RunAsync();
    }
}
