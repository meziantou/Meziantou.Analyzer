using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotUseImplicitCultureSensitiveToStringAnalyzer,
    Meziantou.Analyzer.Rules.DoNotUseImplicitCultureSensitiveToStringInterpolationFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.AddMeziantouAnnotations();
        return test;
    }

#if ROSLYN_5_9_OR_GREATER
    private static CodeFixTest CreateUnionTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.Preview;
        test.TestState.AddMeziantouAnnotations();
        return test;
    }
#endif



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
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A() { _ = {{left}} + {|MA0075:{{right}}|}; }
            }
            """;

        await test.RunAsync();

        var invertedTest = CreateTest();
        invertedTest.TestCode = $$"""
            class Test
            {
                void A() { _ = {|MA0075:{{right}}|} + {{left}}; }
            }
            """;

        await invertedTest.RunAsync();

        var multiConcatTest = CreateTest();
        multiConcatTest.TestCode = $$"""
            class Test
            {
                void A() { string value = ""; value += {{left}} + {|MA0075:{{right}}|}; }
            }
            """;

        await multiConcatTest.RunAsync();
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
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A() { _ = {{left}} + {{right}}; }
            }
            """;

        await test.RunAsync();

        var invertedTest = CreateTest();
        invertedTest.TestCode = $$"""
            class Test
            {
                void A() { _ = {{right}} + {{left}}; }
            }
            """;

        await invertedTest.RunAsync();
    }

    [Fact]
    public Task Concat_Char_String_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(char[] c)
                {
                    string str = "";
                    str = c[0] + str;
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("abc{|MA0076:{(sbyte)-1}|}")]
    [InlineData("abc{|MA0076:{(short)-1}|}")]
    [InlineData("abc{|MA0076:{(int)-1}|}")]
    [InlineData("abc{|MA0076:{(long)-1}|}")]
    [InlineData("abc{|MA0076:{(long?)-1}|}")]
    [InlineData("abc{|MA0076:{(float)-1}|}")]
    [InlineData("abc{|MA0076:{(double)-1}|}")]
    [InlineData("abc{|MA0076:{(decimal)-1}|}")]
    [InlineData("abc{|MA0076:{(float)0}|}")]
    [InlineData("abc{|MA0076:{(double)0}|}")]
    [InlineData("abc{|MA0076:{(decimal)0}|}")]
    [InlineData("abc{|MA0076:{(float)1}|}")]
    [InlineData("abc{|MA0076:{(double)1}|}")]
    [InlineData("abc{|MA0076:{(decimal)1}|}")]
    [InlineData(@"test{|MA0076:{new int[0].Min()}|}")]
    [InlineData(@"test{|MA0076:{System.Int128.One}|}")]
    [InlineData(@"test{|MA0076:{new System.DateOnly(2023,1,1)}|}")]
    public Task InterpolatedStringDiagnostic(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                void A() { string str = $"{{content}}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringDiagnostic_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(int value)
                {
                    _ = $"abc{|MA0076:{value}|}";
                }
            }
            """;
        test.FixedCode = """
            using System.Globalization;

            class Test
            {
                void A(int value)
                {
                    _ = string.Create(CultureInfo.InvariantCulture, $"abc{value}");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringDiagnostic_NoCodeFix_WhenStringCreateIsUnavailable()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            class Test
            {
                void A(int value)
                {
                    _ = $"abc{|MA0076:{value}|}";
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(int value)
                {
                    _ = $"abc{|MA0076:{value}|}";
                }
            }
            """;

        return test.RunAsync();
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
    public Task InterpolatedStringNoDiagnostic(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                void A() { string str = $"{{content}}"; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("abc{(nint)1}")]
    public Task InterpolatedStringNoDiagnostic_CSharp11(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                void A() { string str = $"{{content}}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A() { System.FormattableString a = $"abc{-1}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_Invariant()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A() { string a = System.FormattableString.Invariant($"abc{1}"); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_Invariant_StringConcat()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string b)
                {
                    _ = System.FormattableString.Invariant($"abc{1:N0}") + b;
                    _ = b + System.FormattableString.Invariant($"abc{1:N0}");
                    _ = System.FormattableString.Invariant($"abc{1:N0}") + System.FormattableString.Invariant($"abc{2:N0}");
                    _ = (System.FormattableString.Invariant($"abc{1:N0}")) + b;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_CurrentCulture_StringConcat()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string b)
                {
                    _ = {|MA0075:System.FormattableString.CurrentCulture($"abc{1:N0}")|} + b;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringConcatFormattableString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A() { var a = "abc" + $"{|MA0076:{-1}|}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringConcat_ToString_Int32ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void ToString() { var a = "abc" + $"{-1}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringConcat_ToString_Int32ToString_ConfigNotExcludeToString()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0076.exclude_tostring_methods", "false");
        test.TestCode = """
            class Test
            {
                void ToString() { var a = "abc" + $"{|MA0076:{-1}|}"; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ObjectToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string A() => {|MA0107:new object().ToString()|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ObjectToString_InterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            sealed class Sample {}

            class Test
            {
                void A()
                {
                    var sample = new Sample();
                    _ = $"Value: {{|MA0107:sample|}}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ObjectToString_InterpolatedStringHandler_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Int32ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string A() => (-1).ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SubClassToString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string A() => new Test().ToString();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("Issue", "https://github.com/meziantou/Meziantou.Analyzer/issues/516")]
    public Task ConcatNoDiagnostic_Char()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var c = '!';
                    _ = "abc" + char.ToLower(c, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConcatNoDiagnostic_Boolean()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    bool? value = null;
                    _ = "=" + (value == true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTypeUsingAssemblyAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime))]

            class Test
            {
                void A()
                {
                    _ = "abc" + new System.DateTime();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTypeUsingAssemblyAttribute_MultipleAttributes()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTypeUsingAssemblyAttribute_WithFormat()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "abc")]

            class Test
            {
                void A()
                {
                    _ = $"abc{new System.DateTime():abc}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(""" $"abc{new System.DateTime()}" """)]
    [InlineData(""" $"abc{|MA0076:{new System.DateTime():a}|}" """)]
    public Task IgnoreTypeUsingAssemblyAttribute_WithFormat_DefaultFormatInvariant(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), isDefaultFormatCultureInsensitive: true)]

            class Test
            {
                void A()
                {
                    _ = {{content}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(""" $"abc{|MA0076:{new System.DateTime()}|}" """)]
    [InlineData(""" $"abc{|MA0076:{new System.DateTime():a}|}" """)]
    public Task IgnoreTypeUsingAssemblyAttribute_WithFormat_DefaultFormatCultureSensitive(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), isDefaultFormatCultureInsensitive: false)]

            class Test
            {
                void A()
                {
                    _ = {{content}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTypeUsingAssemblyAttribute_WithFormatNotMatchingAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "abc")]

            class Test
            {
                void A()
                {
                    _ = $"abc{|MA0076:{new System.DateTime():other}|}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTypeUsingAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Property()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = "abc" + Value;
                    _ = $"abc{Value}";
                    _ = Value.ToString();
                    _ = "abc" + {|MA0075:OtherValue|};
                    _ = $"abc{|MA0076:{OtherValue}|}";
                }

                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                double Value => 0;

                double OtherValue => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Field()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                double _value;

                void A()
                {
                    _ = "abc" + _value;
                    _ = $"abc{_value}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Parameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A([Meziantou.Analyzer.Annotations.CultureInsensitive] double value, double other)
                {
                    _ = "abc" + value;
                    _ = $"abc{value}";
                    _ = "abc" + {|MA0075:other|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Parameter_Argument()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(double value)
                {
                    Write($"abc{value}");
                    Write("abc" + value);
                    WriteOther($"abc{|MA0076:{value}|}");
                    WriteOther("abc" + {|MA0075:value|});
                }

                static void Write([Meziantou.Analyzer.Annotations.CultureInsensitive] string value) { }
                static void WriteOther(string value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Parameter_InterpolatedStringHandlerArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;

            class Test
            {
                void A(double value)
                {
                    Write($"abc{"x" + value}");
                    WriteOther($"abc{"x" + {|MA0075:value|}}");
                }

                static void Write([Meziantou.Analyzer.Annotations.CultureInsensitive] CustomHandler handler) { }
                static void WriteOther(CustomHandler handler) { }
            }

            [InterpolatedStringHandler]
            ref struct CustomHandler
            {
                public CustomHandler(int literalLength, int formattedCount) { }
                public void AppendLiteral(string value) { }
                public void AppendFormatted<T>(T value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Parameter_NestedArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(double value)
                {
                    Write(Identity($"abc{|MA0076:{value}|}"));
                }

                static string Identity(string value) => value;
                static void Write([Meziantou.Analyzer.Annotations.CultureInsensitive] string value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_AssemblyAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitive("P:Test.Value")]
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitive("F:Test.Field")]

            class Test
            {
                void A()
                {
                    _ = "abc" + Value;
                    _ = "abc" + Field;
                    _ = "abc" + {|MA0075:OtherValue|};
                }

                static double Value => 0;
                static double Field;
                static double OtherValue => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CustomTypeImplementingIFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = "abc" + {|MA0075:new Sample()|};
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

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_ConditionalExpression_CultureInsensitiveBranches_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_ConditionalExpression_CultureSensitiveType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(bool condition, double a, double b)
                {
                    _ = "test" + ({|MA0075:condition ? a : b|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_CoalesceExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string value)
                {
                    _ = "test" + (value ?? "");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_CoalesceExpression_CultureSensitiveType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(double? value)
                {
                    _ = "test" + ({|MA0075:value ?? 1.5|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_SwitchExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(int value)
                {
                    _ = "test" + (value switch { 0 => "a", _ => "b" });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_SwitchExpression_CultureSensitiveType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(int value)
                {
                    _ = "test" + ({|MA0075:value switch { 0 => 1.5, _ => 2.5 }|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_AwaitExpression_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A(Task<string> task)
                {
                    _ = "test" + await task;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_AwaitExpression_CultureSensitiveType()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class Test
            {
                async Task A(Task<double> task)
                {
                    _ = "test" + {|MA0075:await task|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Object_Concat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(object value)
                {
                    _ = "Value: " + value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Object_InterpolatedString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(object value)
                {
                    _ = $"Value: {value}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Object_Concat_TreatOpaqueRuntimeTypesAsCultureSensitive()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0075.treat_opaque_runtime_types_as_culture_sensitive", "true");
        test.TestCode = """
            class Test
            {
                void A(object value)
                {
                    _ = "Value: " + {|MA0075:value|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Object_InterpolatedString_TreatOpaqueRuntimeTypesAsCultureSensitive()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0076.treat_opaque_runtime_types_as_culture_sensitive", "true");
        test.TestCode = """
            class Test
            {
                void A(object value)
                {
                    _ = $"Value: {|MA0076:{value}|}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_Concat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(IValue value)
                {
                    _ = "Value: " + value;
                }
            }

            interface IValue { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_InterpolatedString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(IValue value)
                {
                    _ = $"Value: {value}";
                }
            }

            interface IValue { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IFormattable_Concat()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(System.IFormattable value)
                {
                    _ = "Value: " + {|MA0075:value|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IFormattable_InterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(System.IFormattable value)
                {
                    _ = $"Value: {|MA0076:{value}|}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonSealedType_Concat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(Value value)
                {
                    _ = "Value: " + value;
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonSealedType_Concat_TreatUnsealedTypesAsCultureSensitive()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0075.treat_unsealed_types_as_culture_sensitive", "true");
        test.TestCode = """
            class Test
            {
                void A(Value value)
                {
                    _ = "Value: " + {|MA0075:value|};
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonSealedType_InterpolatedString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(Value value)
                {
                    _ = $"Value: {value}";
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonSealedType_InterpolatedString_TreatUnsealedTypesAsCultureSensitive()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0076.treat_unsealed_types_as_culture_sensitive", "true");
        test.TestCode = """
            class Test
            {
                void A(Value value)
                {
                    _ = $"Value: {|MA0076:{value}|}";
                }
            }

            class Value { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedNonFormattableType_Concat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task SealedNonFormattableType_InterpolatedString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task UnconstrainedTypeParameter_Concat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T value)
                {
                    _ = "Value: " + value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task UnconstrainedTypeParameter_InterpolatedString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T value)
                {
                    _ = $"Value: {value}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToIFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T item) where T : System.IFormattable
                {
                    _ = "abc" + {|MA0075:item|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToISpanFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T item) where T : System.ISpanFormattable
                {
                    _ = "abc" + {|MA0075:item|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToTypeThatImplementsIFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T item) where T : Sample
                {
                    _ = "abc" + {|MA0075:item|};
                }
            }

            class Sample : System.IFormattable
            {
                public string ToString(string? format, System.IFormatProvider? formatProvider) => "abc";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToTypeThatImplementsISpanFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T item) where T : Sample
                {
                    _ = "abc" + {|MA0075:item|};
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

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToTypeThatHasToStringWithIFormatProvider()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A<T>(T item) where T : Sample
                {
                    _ = "abc" + {|MA0075:item|};
                }
            }

            class Sample
            {
                public string ToString(System.IFormatProvider? formatProvider) => "abc";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericTypeParameterConstrainedToTypeWithCultureInsensitiveAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value")]
    [InlineData("value.ToString()")]
    [InlineData("value.ToString(\"G\")")]
    [InlineData("value.ToString(\"g\")")]
    [InlineData("value.ToString(\"F\")")]
    [InlineData("value.ToString(\"f\")")]
    [InlineData("value.ToString(\"D\")")]
    [InlineData("value.ToString(\"d\")")]
    [InlineData("value.ToString(\"X\")")]
    [InlineData("value.ToString(\"x\")")]
    [InlineData("value.ToString(default(string))")]
    [InlineData("value.ToString(format)")]
    public Task Concat_Enum_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(StringComparison value, string format) { _ = "abc" + {{expression}}; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value")]
    [InlineData("value.ToString()")]
    [InlineData("value?.ToString(\"G\")")]
    [InlineData("value.Value.ToString(\"G\")")]
    public Task Concat_NullableEnum_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(StringComparison? value) { _ = "abc" + {{expression}}; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value.ToString()")]
    [InlineData("value.ToString(\"G\")")]
    public Task Concat_SystemEnum_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(Enum value) { _ = "abc" + {{expression}}; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value.ToString(\"G\")")]
    [InlineData("value.ToString(\"F\")")]
    public Task Concat_UserDefinedEnum_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Test
            {
                void A(Sample value) { _ = "abc" + {{expression}}; }
            }

            enum Sample { A }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("{value}")]
    [InlineData("{value:G}")]
    [InlineData("{value:F}")]
    [InlineData("{value.ToString()}")]
    [InlineData("{value.ToString(\"G\")}")]
    public Task InterpolatedString_Enum_NoDiagnostic(string content)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(StringComparison value) { _ = $"abc{{content}}"; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value.ToString()")]
    [InlineData("value.ToString(\"G\")")]
    public Task Concat_EnumInGenericMethod_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A<T>(T value) where T : struct, Enum { _ = "abc" + {{expression}}; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value?.ToString()")]
    [InlineData("value?.ToString(\"F\")")]
    [InlineData("value?.ToString(format)")]
    [InlineData("value?.Date.ToString(\"F\")")]
    [InlineData("value?.Ticks.ToString()")]
    public Task Concat_ConditionalAccess_Diagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(DateTime? value, string format) { _ = "abc" + {|MA0075:{{expression}}|}; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("value?.ToString(\"o\")")]
    [InlineData("value?.ToString(System.Globalization.CultureInfo.InvariantCulture)")]
    [InlineData("value?.Ticks.ToString(\"X\")")]
    [InlineData("value?.Kind.ToString(\"G\")")]
    public Task Concat_ConditionalAccess_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(DateTime? value) { _ = "abc" + {{expression}}; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_NestedConditionalAccess_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A(Test value) { _ = "abc" + {|MA0075:value?.Child?.Value.ToString("F")|}; }

                Test Child { get; }
                DateTime Value { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_ConditionalAccessToCultureSensitiveMember_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A(Test value) { _ = "abc" + {|MA0075:value?.Value|}; }

                DateTime Value { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_ConditionalAccess_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Test
            {
                void A(Test value) { _ = $"abc{|MA0076:{value?.Value}|}"; }

                DateTime Value { get; }
            }
            """;

        return test.RunAsync();
    }

#if ROSLYN_5_9_OR_GREATER

    [Fact]
    public Task Union_AllCaseTypesAreCultureInsensitive()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + value;
                }
            }

            union Sample(bool, string);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Union_OneCaseTypeIsCultureSensitive()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + {|MA0075:value|};
                }
            }

            union Sample(bool, double);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Union_CaseTypeIsAUnionWithACultureSensitiveCaseType()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + {|MA0075:value|};
                }
            }

            union Sample(bool, Inner);
            union Inner(string, double);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Union_CaseTypesReferenceEachOther()
    {
        var test = CreateUnionTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Union_NullableCaseType()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + {|MA0075:value|};
                }
            }

            union Sample(bool, double?);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Union_CultureInsensitiveTypeAttribute()
    {
        var test = CreateUnionTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Union_CustomUnionType()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + {|MA0075:value|};
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

        return test.RunAsync();
    }

    [Fact]
    public Task Union_UnionMemberProvider()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = "abc" + {|MA0075:value|};
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

        return test.RunAsync();
    }

    [Fact]
    public Task Union_InterpolatedStringWithCultureSensitiveCaseType()
    {
        var test = CreateUnionTest();
        test.TestCode = """
            class Test
            {
                void A(Sample value)
                {
                    _ = $"{|MA0076:{value}|}";
                }
            }

            union Sample(bool, System.DateTime)
            {
                public override string ToString() => "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Union_InterpolatedStringWithCultureInsensitiveFormat()
    {
        var test = CreateUnionTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

#endif
}
