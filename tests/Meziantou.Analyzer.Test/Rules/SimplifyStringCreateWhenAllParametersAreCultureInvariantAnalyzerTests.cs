namespace Meziantou.Analyzer.Test.Rules;

public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzer>()
            .WithCodeFixProvider<SimplifyStringCreateWhenAllParametersAreCultureInvariantFixer>()
            .AddMeziantouAttributes()
            .WithTargetFramework(TargetFramework.Net6_0);
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_OnlyCultureInvariantParameters_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:O}.")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithString_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var name = "test";
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Name: {name}")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithGuid_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var id = Guid.NewGuid();
        var x = [|string.Create(CultureInfo.InvariantCulture, $"ID: {id}")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithTimeSpanInvariantFormat_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var duration = TimeSpan.FromSeconds(42);
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Duration: {duration:c}")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithCultureSensitiveParameter_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithDateTimeNonInvariantFormat_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithCurrentCulture_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_LiteralOnly_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Hello World")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithInteger_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithNullableInteger_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithNullableDouble_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_Object_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(object value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_Object_TreatOpaqueRuntimeTypesAsCultureSensitive_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0185.treat_opaque_runtime_types_as_culture_sensitive", "true")
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_Interface_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(IValue value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}

interface IValue { }
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_IFormattable_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_NonSealedType_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(Value value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}

class Value { }
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_NonSealedType_TreatUnsealedTypesAsCultureSensitive_NoDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0185.treat_unsealed_types_as_culture_sensitive", "true")
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_UnconstrainedTypeParameter_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test<T>(T value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_SealedNonFormattableType_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(Value value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}

sealed class Value
{
    public override string ToString() => string.Empty;
}
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_CultureInsensitiveTypeAttribute_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test(Value value)
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {value}")|];
    }
}

[Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
class Value : IFormattable
{
    public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
}
""";
        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_CultureInsensitiveAttribute_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        _ = [|string.Create(CultureInfo.InvariantCulture, $"Value: {GetValue()}")|];
    }

    [Meziantou.Analyzer.Annotations.CultureInsensitive]
    static double GetValue() => 0;
}
""";
        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        _ = $"Value: {GetValue()}";
    }

    [Meziantou.Analyzer.Annotations.CultureInsensitive]
    static double GetValue() => 0;
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_EmptyString_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_MultipleWords_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"This is a test message without any interpolations")|];
    }
}
""";

        const string Fix = """
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
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("{value}")]
    [InlineData("{value:G}")]
    [InlineData("{value.ToString()}")]
    [InlineData("{value.ToString(\"G\")}")]
    public async Task StringCreateWithInvariantCulture_Enum_ShouldReport(string content)
    {
        var sourceCode = $$"""
using System;
using System.Globalization;

class TypeName
{
    public void Test(StringComparison value)
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"abc{{content}}")|];
    }
}
""";

        var fix = $$"""
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }
}
