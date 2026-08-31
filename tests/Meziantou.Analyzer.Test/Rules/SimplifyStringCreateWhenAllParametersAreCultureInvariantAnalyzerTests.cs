using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzer,
    Meziantou.Analyzer.Rules.SimplifyStringCreateWhenAllParametersAreCultureInvariantFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.AddMeziantouAnnotations();
        return test;
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_OnlyCultureInvariantParameters_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:O}.")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = $"Current time is {DateTime.Now:O}.";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithString_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var name = "test";
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Name: {name}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var name = "test";
                    var x = $"Name: {name}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithGuid_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var id = Guid.NewGuid();
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"ID: {id}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var id = Guid.NewGuid();
                    var x = $"ID: {id}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithTimeSpanInvariantFormat_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var duration = TimeSpan.FromSeconds(42);
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Duration: {duration:c}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var duration = TimeSpan.FromSeconds(42);
                    var x = $"Duration: {duration:c}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithCultureSensitiveParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var price = 42.5;
                    var x = string.Create(CultureInfo.InvariantCulture, $"Price: {price}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithDateTimeNonInvariantFormat_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:d}.");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithCurrentCulture_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = string.Create(CultureInfo.CurrentCulture, $"Current time is {DateTime.Now:O}.");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_LiteralOnly_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Hello World")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = $"Hello World";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithInteger_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var count = 42;
                    var x = string.Create(CultureInfo.InvariantCulture, $"Count: {count}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithNullableInteger_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    int? n = 42;
                    var s = string.Create(CultureInfo.InvariantCulture, $"/v{n}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_WithNullableDouble_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    double? value = 3.14;
                    var s = string.Create(CultureInfo.InvariantCulture, $"Value: {value}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_Object_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(object value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(object value)
                {
                    _ = $"Value: {value}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_Object_TreatOpaqueRuntimeTypesAsCultureSensitive_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestState.SetConfiguration("MA0185.treat_opaque_runtime_types_as_culture_sensitive", "true");
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(object value)
                {
                    _ = string.Create(CultureInfo.InvariantCulture, $"Value: {value}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_Interface_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(IValue value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }

            interface IValue { }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(IValue value)
                {
                    _ = $"Value: {value}";
                }
            }

            interface IValue { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_IFormattable_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(IFormattable value)
                {
                    _ = string.Create(CultureInfo.InvariantCulture, $"Value: {value}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_NonSealedType_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }

            class Value { }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = $"Value: {value}";
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_NonSealedType_TreatUnsealedTypesAsCultureSensitive_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestState.SetConfiguration("MA0185.treat_unsealed_types_as_culture_sensitive", "true");
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = string.Create(CultureInfo.InvariantCulture, $"Value: {value}");
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_UnconstrainedTypeParameter_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test<T>(T value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test<T>(T value)
                {
                    _ = $"Value: {value}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_SealedNonFormattableType_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }

            sealed class Value
            {
                public override string ToString() => string.Empty;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = $"Value: {value}";
                }
            }

            sealed class Value
            {
                public override string ToString() => string.Empty;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_CultureInsensitiveTypeAttribute_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|};
                }
            }

            [Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
            class Value : IFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(Value value)
                {
                    _ = $"Value: {value}";
                }
            }

            [Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
            class Value : IFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_CultureInsensitiveAttribute_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    _ = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"Value: {Value}")|};
                }

                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                static double Value => 0;
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    _ = $"Value: {Value}";
                }

                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                static double Value => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_EmptyString_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = $"";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringCreateWithInvariantCulture_MultipleWords_ShouldReport()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"This is a test message without any interpolations")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    var x = $"This is a test message without any interpolations";
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("{value}")]
    [InlineData("{value:G}")]
    [InlineData("{value.ToString()}")]
    [InlineData("{value.ToString(\"G\")}")]
    public Task StringCreateWithInvariantCulture_Enum_ShouldReport(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(StringComparison value)
                {
                    var x = {|MA0185:string.Create(CultureInfo.InvariantCulture, $"abc{{content}}")|};
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test(StringComparison value)
                {
                    var x = $"abc{{content}}";
                }
            }
            """;

        return test.RunAsync();
    }
}
