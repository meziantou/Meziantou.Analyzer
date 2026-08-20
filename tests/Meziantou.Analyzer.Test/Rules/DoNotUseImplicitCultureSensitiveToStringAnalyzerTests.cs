namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseImplicitCultureSensitiveToStringAnalyzer>()
            .WithCodeFixProvider<DoNotUseImplicitCultureSensitiveToStringInterpolationFixer>()
            .AddMeziantouAttributes()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Theory]
    [InlineData("\"abc\"", "0f")]
    [InlineData("\"abc\"", "(float)0")]
    [InlineData("\"abc\"", "0d")]
    [InlineData("\"abc\"", "(double)0")]
    [InlineData("\"abc\"", "0m")]
    [InlineData("\"abc\"", "(decimal)0")]
    [InlineData("\"abc\"", "1f")]
    [InlineData("\"abc\"", "(float)1")]
    [InlineData("\"abc\"", "1d")]
    [InlineData("\"abc\"", "(double)1")]
    [InlineData("\"abc\"", "1m")]
    [InlineData("\"abc\"", "(decimal)1")]
    [InlineData("\"\"", "-1")]
    [InlineData("\"abc\"", "-1")]
    [InlineData("\"abc\"", "(sbyte)-1")]
    [InlineData("\"abc\"", "(short)-1")]
    [InlineData("\"abc\"", "(int)-1")]
    [InlineData("\"abc\"", "-1L")]
    [InlineData("\"abc\"", "(long)-1")]
    [InlineData("\"abc\"", "-1f")]
    [InlineData("\"abc\"", "(float)-1")]
    [InlineData("\"abc\"", "-1d")]
    [InlineData("\"abc\"", "(double)-1")]
    [InlineData("\"abc\"", "-1m")]
    [InlineData("\"abc\"", "(decimal)-1")]
    [InlineData("\"abc\"", "default(System.DateTime)")]
    [InlineData("\"abc\"", "default(System.DateTimeOffset)")]
    [InlineData("\"abc\"", "default(System.FormattableString)")]
    [InlineData("\"abc\"", "default(System.Numerics.BigInteger)")]
    [InlineData("\"abc\"", "default(System.Numerics.Complex)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector2)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector3)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector4)")]
    [InlineData("\"abc\"", "default(System.Numerics.Vector<int>)")]
    public async Task ConcatDiagnostic(string left, string right)
    {
        var sourceCode = $$"""
            class Test
            {
                void A() { _ = {{left}} + [|{{right}}|]; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();

        var invertedSourceCode = $$"""
            class Test
            {
                void A() { _ = [|{{right}}|] + {{left}}; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(invertedSourceCode)
              .ValidateAsync();

        var multiConcat = $$"""
        class Test
        {
            void A() { string value = ""; value += {{left}} + [|{{right}}|]; }
        }
        """;
        await CreateProjectBuilder()
              .WithSourceCode(multiConcat)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("\"abc\"", "'d'")]
    [InlineData("\"abc\"", "\"def\"")]
    [InlineData("\"abc\"", "(byte)1")]
    [InlineData("\"abc\"", "1u")]
    [InlineData("\"abc\"", "(ushort)1")]
    [InlineData("\"abc\"", "1ul")]
    [InlineData("\"abc\"", "(ulong)1")]
    [InlineData("\"abc\"", "(sbyte)1")]
    [InlineData("\"abc\"", "(short)1")]
    [InlineData("\"abc\"", "1")]
    [InlineData("\"abc\"", "(int)1")]
    [InlineData("\"abc\"", "1L")]
    [InlineData("\"abc\"", "(long)1")]
    [InlineData("\"abc\"", "(long?)1")]
    [InlineData("\"abc\"", "(System.UInt128)1")]
    [InlineData("\"abc\"", "new System.Guid()")]
    [InlineData("\"abc\"", "new System.TimeSpan()")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"c\")")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"t\")")]
    [InlineData("\"abc\"", "System.TimeSpan.Zero.ToString(\"T\")")]
    [InlineData("\"abc\"", "new System.Uri(\"\")")]
    [InlineData("\"abc\"", @"$""test{new System.Uri("""")}""")]
    [InlineData("\"abc\"", @"' '")]
    [InlineData("\"abc\"", "default(System.Uri)")]
    public async Task ConcatNoDiagnostic(string left, string right)
    {
        var sourceCode = $$"""
            class Test
            {
                void A() { _ = {{left}} + {{right}}; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();

        var invertedSourceCode = $$"""
            class Test
            {
                void A() { _ = {{right}} + {{left}}; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(invertedSourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_Char_String_NoDiagnostic()
    {
        var sourceCode = """
            class Test
            {
                void A(char[] c)
                {
                    string str = "";
                    str = c[0] + str;
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("abc[|{(sbyte)-1}|]")]
    [InlineData("abc[|{(short)-1}|]")]
    [InlineData("abc[|{(int)-1}|]")]
    [InlineData("abc[|{(long)-1}|]")]
    [InlineData("abc[|{(long?)-1}|]")]
    [InlineData("abc[|{(float)-1}|]")]
    [InlineData("abc[|{(double)-1}|]")]
    [InlineData("abc[|{(decimal)-1}|]")]
    [InlineData("abc[|{(float)0}|]")]
    [InlineData("abc[|{(double)0}|]")]
    [InlineData("abc[|{(decimal)0}|]")]
    [InlineData("abc[|{(float)1}|]")]
    [InlineData("abc[|{(double)1}|]")]
    [InlineData("abc[|{(decimal)1}|]")]
    [InlineData(@"test[|{new int[0].Min()}|]")]
    [InlineData(@"test[|{System.Int128.One}|]")]
    [InlineData(@"test[|{new System.DateOnly(2023,1,1)}|]")]
    public async Task InterpolatedStringDiagnostic(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringDiagnostic_CodeFix()
    {
        const string SourceCode = """
class Test
{
    void A(int value)
    {
        _ = $"abc[|{value}|]";
    }
}
""";

        const string Fix = """
using System.Globalization;

class Test
{
    void A(int value)
    {
        _ = string.Create(CultureInfo.InvariantCulture, $"abc{value}");
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
    public async Task InterpolatedStringDiagnostic_NoCodeFix_WhenStringCreateIsUnavailable()
    {
        const string SourceCode = """
class Test
{
    void A(int value)
    {
        _ = $"abc[|{value}|]";
    }
}
""";

        const string Fix = """
class Test
{
    void A(int value)
    {
        _ = $"abc{value}";
    }
}
""";

        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc{\"def\"}")]
    [InlineData("abc{'a'}")]
    [InlineData("abc{(byte)1}")]
    [InlineData("abc{(ushort)1}")]
    [InlineData("abc{(uint)1}")]
    [InlineData("abc{(ulong)1}")]
    [InlineData(@"test{new System.Uri("""")}")]
    [InlineData(@"test{new int[0].Length}")]
    [InlineData(@"test{new int[0].Count()}")]
    [InlineData(@"test{new System.Collections.Generic.List<int>().Count}")]
    [InlineData(@"test{new System.DateOnly(2023,1,1):o}")]
    public async Task InterpolatedStringNoDiagnostic(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("abc{(nint)1}")]
    public async Task InterpolatedStringNoDiagnostic_CSharp11(string content)
    {
        var sourceCode = @"using System.Linq;
class Test
{
    void A() { string str = $""" + content + @"""; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString()
    {
        var sourceCode = """
            class Test
            {
                void A() { System.FormattableString a = $"abc{-1}"; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task FormattableString_Invariant()
    {
        var sourceCode = """
            class Test
            {
                void A() { string a = System.FormattableString.Invariant($"abc{1}"); }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringConcatFormattableString()
    {
        var sourceCode = """
            class Test
            {
                void A() { var a = "abc" + $"[|{-1}|]"; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringConcat_ToString_Int32ToString()
    {
        var sourceCode = """
            class Test
            {
                void ToString() { var a = "abc" + $"{-1}"; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringConcat_ToString_Int32ToString_ConfigNotExcludeToString()
    {
        var sourceCode = """
            class Test
            {
                void ToString() { var a = "abc" + $"[|{-1}|]"; }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .AddAnalyzerConfiguration("MA0076.exclude_tostring_methods", "false")
              .ValidateAsync();
    }

    [Fact]
    public async Task ObjectToString()
    {
        var sourceCode = """
            class Test
            {
                string A() => [|new object().ToString()|];
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ObjectToString_InterpolatedString()
    {
        var sourceCode = """
            sealed class Sample {}

            class Test
            {
                void A()
                {
                    var sample = new Sample();
                    _ = $"Value: {[|sample|]}";
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ObjectToString_InterpolatedStringHandler_NoDiagnostic()
    {
        var sourceCode = """
            using System.Runtime.CompilerServices;

            class Test
            {
                void A()
                {
                    var sample = new Sample();
                    Write($"Value: {sample}");
                }

                static void Write(CustomHandler handler) { }
            }

            sealed class Sample {}

            [InterpolatedStringHandler]
            ref struct CustomHandler
            {
                public CustomHandler(int literalLength, int formattedCount) { }
                public void AppendLiteral(string value) { }
                public void AppendFormatted<T>(T value) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32ToString()
    {
        var sourceCode = """
            class Test
            {
                string A() => (-1).ToString();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SubClassToString()
    {
        var sourceCode = """
            class Test
            {
                string A() => new Test().ToString();
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/516")]
    public async Task ConcatNoDiagnostic_Char()
    {
        var sourceCode = """
class Test
{
    void A()
    {
        var c = '!';
        _ = "abc" + char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ConcatNoDiagnostic_Boolean()
    {
        var sourceCode = """
class Test
{
    void A()
    {
        bool? value = null;
        _ = "=" + (value == true);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IgnoreTypeUsingAssemblyAttribute()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime))]

class Test
{
    void A()
    {
        _ = "abc" + new System.DateTime();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IgnoreTypeUsingAssemblyAttribute_MultipleAttributes()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "a")]
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "b")]
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "c")]

class Test
{
    void A()
    {
        _ = $"abc{new System.DateTime():b}";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IgnoreTypeUsingAssemblyAttribute_WithFormat()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "abc")]

class Test
{
    void A()
    {
        _ = $"abc{new System.DateTime():abc}";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(""" $"abc{new System.DateTime()}" """)]
    [InlineData(""" $"abc[|{new System.DateTime():a}|]" """)]
    public async Task IgnoreTypeUsingAssemblyAttribute_WithFormat_DefaultFormatInvariant(string content)
    {
        var sourceCode = $$"""
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), isDefaultFormatCultureInsensitive: true)]

class Test
{
    void A()
    {
        _ = {{content}};
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(""" $"abc[|{new System.DateTime()}|]" """)]
    [InlineData(""" $"abc[|{new System.DateTime():a}|]" """)]
    public async Task IgnoreTypeUsingAssemblyAttribute_WithFormat_DefaultFormatCultureSensitive(string content)
    {
        var sourceCode = $$"""
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), isDefaultFormatCultureInsensitive: false)]

class Test
{
    void A()
    {
        _ = {{content}};
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IgnoreTypeUsingAssemblyAttribute_WithFormatNotMatchingAttribute()
    {
        var sourceCode = """
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "abc")]

class Test
{
    void A()
    {
        _ = $"abc[|{new System.DateTime():other}|]";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IgnoreTypeUsingAttribute()
    {
        var sourceCode = """
class Test
{
    void A()
    {
        _ = "abc" + new Sample();
    }
}

[Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
class Sample : System.IFormattable
{
    public string ToString(string? format, System.IFormatProvider? formatProvider)
    {
        return "abc";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CustomTypeImplementingIFormattable()
    {
        var sourceCode = """
class Test
{
    void A()
    {
        _ = "abc" + [|new Sample()|];
    }
}

class Sample : System.IFormattable
{
    public string ToString(string? format, System.IFormatProvider? formatProvider)
    {
        return "abc";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_ConditionalExpression_CultureInsensitiveBranches_NoDiagnostic()
    {
        var sourceCode = """
using System;
using System.Globalization;
class Test
{
    void A(DateTime? date)
    {
        _ = "test" + (date.HasValue ? date.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_ConditionalExpression_CultureSensitiveType()
    {
        var sourceCode = """
class Test
{
    void A(bool condition, double a, double b)
    {
        _ = "test" + ([|condition ? a : b|]);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_CoalesceExpression_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(string value)
    {
        _ = "test" + (value ?? "");
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_CoalesceExpression_CultureSensitiveType()
    {
        var sourceCode = """
class Test
{
    void A(double? value)
    {
        _ = "test" + ([|value ?? 1.5|]);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_SwitchExpression_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(int value)
    {
        _ = "test" + (value switch { 0 => "a", _ => "b" });
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_SwitchExpression_CultureSensitiveType()
    {
        var sourceCode = """
class Test
{
    void A(int value)
    {
        _ = "test" + ([|value switch { 0 => 1.5, _ => 2.5 }|]);
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_AwaitExpression_NoDiagnostic()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    async Task A(Task<string> task)
    {
        _ = "test" + await task;
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_AwaitExpression_CultureSensitiveType()
    {
        var sourceCode = """
using System.Threading.Tasks;
class Test
{
    async Task A(Task<double> task)
    {
        _ = "test" + [|await task|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Object_Concat_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(object value)
    {
        _ = "Value: " + value;
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Object_InterpolatedString_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(object value)
    {
        _ = $"Value: {value}";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Object_Concat_TreatOpaqueRuntimeTypesAsCultureSensitive()
    {
        var sourceCode = """
class Test
{
    void A(object value)
    {
        _ = "Value: " + [|value|];
    }
}
""";
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0075.treat_opaque_runtime_types_as_culture_sensitive", "true")
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Object_InterpolatedString_TreatOpaqueRuntimeTypesAsCultureSensitive()
    {
        var sourceCode = """
class Test
{
    void A(object value)
    {
        _ = $"Value: [|{value}|]";
    }
}
""";
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0076.treat_opaque_runtime_types_as_culture_sensitive", "true")
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_Concat_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(IValue value)
    {
        _ = "Value: " + value;
    }
}

interface IValue { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_InterpolatedString_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(IValue value)
    {
        _ = $"Value: {value}";
    }
}

interface IValue { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IFormattable_Concat()
    {
        var sourceCode = """
class Test
{
    void A(System.IFormattable value)
    {
        _ = "Value: " + [|value|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task IFormattable_InterpolatedString()
    {
        var sourceCode = """
class Test
{
    void A(System.IFormattable value)
    {
        _ = $"Value: [|{value}|]";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonSealedType_Concat_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
    {
        _ = "Value: " + value;
    }
}

class Value { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonSealedType_Concat_TreatUnsealedTypesAsCultureSensitive()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
    {
        _ = "Value: " + [|value|];
    }
}

class Value { }
""";
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0075.treat_unsealed_types_as_culture_sensitive", "true")
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonSealedType_InterpolatedString_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
    {
        _ = $"Value: {value}";
    }
}

class Value { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonSealedType_InterpolatedString_TreatUnsealedTypesAsCultureSensitive()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
    {
        _ = $"Value: [|{value}|]";
    }
}

class Value { }
""";
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0076.treat_unsealed_types_as_culture_sensitive", "true")
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedNonFormattableType_Concat_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
    {
        _ = "Value: " + value;
    }
}

sealed class Value
{
    public override string ToString() => string.Empty;
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedNonFormattableType_InterpolatedString_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A(Value value)
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
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnconstrainedTypeParameter_Concat_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A<T>(T value)
    {
        _ = "Value: " + value;
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task UnconstrainedTypeParameter_InterpolatedString_NoDiagnostic()
    {
        var sourceCode = """
class Test
{
    void A<T>(T value)
    {
        _ = $"Value: {value}";
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToIFormattable()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : System.IFormattable
    {
        _ = "abc" + [|item|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToISpanFormattable()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : System.ISpanFormattable
    {
        _ = "abc" + [|item|];
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToTypeThatImplementsIFormattable()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : Sample
    {
        _ = "abc" + [|item|];
    }
}

class Sample : System.IFormattable
{
    public string ToString(string? format, System.IFormatProvider? formatProvider) => "abc";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToTypeThatImplementsISpanFormattable()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : Sample
    {
        _ = "abc" + [|item|];
    }
}

class Sample : System.ISpanFormattable
{
    public override string ToString() => "abc";
    public string ToString(string? format, System.IFormatProvider? formatProvider) => "abc";
    public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider)
    {
        charsWritten = 0;
        return true;
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToTypeThatHasToStringWithIFormatProvider()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : Sample
    {
        _ = "abc" + [|item|];
    }
}

class Sample
{
    public string ToString(System.IFormatProvider? formatProvider) => "abc";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericTypeParameterConstrainedToTypeWithCultureInsensitiveAttribute()
    {
        var sourceCode = """
class Test
{
    void A<T>(T item) where T : Sample
    {
        _ = "abc" + item;
    }
}

[Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
class Sample : System.IFormattable
{
    public string ToString(string? format, System.IFormatProvider? formatProvider) => "abc";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if ROSLYN_5_9_OR_GREATER
    private static ProjectBuilder CreateUnionProjectBuilder()
    {
        return CreateProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }

    [Fact]
    public async Task Union_AllCaseTypesAreCultureInsensitive()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + value;
    }
}

union Sample(bool, string);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_OneCaseTypeIsCultureSensitive()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + [|value|];
    }
}

union Sample(bool, double);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_CaseTypeIsAUnionWithACultureSensitiveCaseType()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + [|value|];
    }
}

union Sample(bool, Inner);
union Inner(string, double);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_CaseTypesReferenceEachOther()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + value;
    }
}

union Sample(bool, Inner);
union Inner(string, Sample);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_NullableCaseType()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + [|value|];
    }
}

union Sample(bool, double?);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_CultureInsensitiveTypeAttribute()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + value;
    }
}

[Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute]
union Sample(bool, double);
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_CustomUnionType()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + [|value|];
    }
}

[System.Runtime.CompilerServices.Union]
struct Sample : System.Runtime.CompilerServices.IUnion
{
    private readonly object _value;

    public Sample(bool value) => _value = value;
    public Sample(double value) => _value = value;

    public object Value => _value;
}
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_UnionMemberProvider()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = "abc" + [|value|];
    }
}

[System.Runtime.CompilerServices.Union]
struct Sample : Sample.IUnionMembers
{
    private readonly object _value;

    private Sample(object value) => _value = value;

    public interface IUnionMembers
    {
        static Sample Create(bool value) => new(value);
        static Sample Create(double value) => new(value);
        object Value { get; }
    }

    object IUnionMembers.Value => _value;
}
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_InterpolatedStringWithCultureSensitiveCaseType()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = $"[|{value}|]";
    }
}

union Sample(bool, System.DateTime)
{
    public override string ToString() => "";
}
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Union_InterpolatedStringWithCultureInsensitiveFormat()
    {
        var sourceCode = """
class Test
{
    void A(Sample value)
    {
        _ = $"{value:o}";
    }
}

union Sample(bool, System.DateTime)
{
    public override string ToString() => "";
}
""";
        await CreateUnionProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif
}
