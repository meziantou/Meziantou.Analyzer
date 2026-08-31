using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseIFormatProviderAnalyzer,
    Meziantou.Analyzer.Rules.UseIFormatProviderFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseIFormatProviderAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestState.AddMeziantouAnnotations();
        return test;
    }

    [Fact]
    public Task Int32ToStringWithCultureInfo_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            1.ToString(System.Globalization.CultureInfo.InvariantCulture);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Int32ToStringWithoutCultureInfo_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|#0:(-1).ToString()|};
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0011", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'ToString' that has a 'System.IFormatProvider' parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task Int32_PositiveToStringWithoutCultureInfo_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            1.ToString();
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(""" (-1).ToString("x") """)]
    [InlineData(""" (-1).ToString("x8") """)]
    [InlineData(""" (-1).ToString("X" )""")]
    [InlineData(""" (-1).ToString("X8") """)]
    [InlineData(""" (-1).ToString("B") """)]
    [InlineData(""" true.ToString() """)]
    [InlineData(""" default(System.Guid).ToString() """)]
    [InlineData(""" default(System.Guid).ToString("D") """)]
    [InlineData(""" System.TimeSpan.Zero.ToString() """)]
    [InlineData(""" System.TimeSpan.Zero.ToString("c") """)]
    [InlineData(""" System.TimeSpan.Zero.ToString("T") """)]
    [InlineData(""" [|System.TimeSpan.Zero.ToString("G")|] """)]
    [InlineData(""" ' '.ToString(); """)]
    [InlineData(""" [|System.DateTime.TryParse("", out _)|] """)]
    [InlineData(""" [|System.DateTimeOffset.TryParse("", out _)|] """)]
    [InlineData(""" [|"".ToLower()|] """)]
    [InlineData(""" [|new System.Text.StringBuilder().AppendFormat("{0}", -1)|] """)]
    [InlineData(""" new System.Text.StringBuilder().AppendFormat("{0}", 10) """)]
    [InlineData(""" new System.Text.StringBuilder().AppendFormat("{0} / {1}", "X", "Y") """)]
    [InlineData(""" System.DayOfWeek.Monday.ToString() """)]
    [InlineData(""" default(System.DateTime).ToString("o") """)]
    [InlineData(""" default(System.DateTime).ToString("O") """)]
    [InlineData(""" default(System.DateTime).ToString("r") """)]
    [InlineData(""" default(System.DateTime).ToString("R") """)]
    [InlineData(""" default(System.DateTime).ToString("s") """)]
    [InlineData(""" default(System.DateTime).ToString("u") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("o") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("O") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("r") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("R") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("s") """)]
    [InlineData(""" default(System.DateTimeOffset).ToString("u") """)]
    [InlineData(""" [|default(System.DateTime).ToString("yyyy")|] """)]
    [InlineData(""" System.Guid.Parse("o") """)]
    [InlineData(""" System.Guid.TryParse("o", out _) """)]
    [InlineData(""" ((int?)1)?.ToString(System.Globalization.CultureInfo.InvariantCulture) """)]
    [InlineData(""" string.Format("", "test", 1, 'c') """)]
    [InlineData(""" string.Format(default(System.IFormatProvider), "", -1) """)]
    [InlineData(""" string.Format("") """)]
    [InlineData(""" [|string.Format("", -1)|] """)]
    [InlineData(""" [|string.Format("", 0, 0, 0, 0, 0, 0, -1, 0 ,0 ,0, 0)|] """)]
    [InlineData(""" System.Convert.ToChar((object)null) """)]
    [InlineData(""" System.Convert.ToChar("") """)]
    [InlineData(""" System.Convert.ToBoolean((object)null) """)]
    [InlineData(""" System.Convert.ToBoolean("") """)]
    public Task Tests(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{expression}};
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SystemTimeSpanImplicitToStringWithoutCultureInfo_InterpolatedString_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            var timeSpan = System.TimeSpan.FromSeconds(1);
            var myString = $"This is a test: {timeSpan}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Int32ParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|#0:int.Parse("")|};
            {|#1:int.Parse("", System.Globalization.NumberStyles.Any)|};
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0011", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter"));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0011", DiagnosticSeverity.Warning).WithLocation(1).WithMessage("Use an overload of 'Parse' that has a 'System.IFormatProvider' parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task Int32ParseWithoutCultureInfo_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            [|int.Parse("")|];
            """;
        test.FixedCode = """
            int.Parse("", System.Globalization.CultureInfo.InvariantCulture);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SingleTryParseWithoutCultureInfo_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            {|#0:float.TryParse("", out _)|};
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0011", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Use an overload of 'TryParse' that has a 'System.IFormatProvider' parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task ListOfCultureInfo_FirstOrDefault_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Globalization;
            using System.Linq;

            List<CultureInfo> values = new();
            _ = values.FirstOrDefault();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ListOfCultureInfo_LastOrDefault_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Globalization;
            using System.Linq;

            List<CultureInfo> values = new();
            _ = values.LastOrDefault();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumToString()
    {
        var test = CreateTest();
        test.TestCode = """
            System.Enum value = default;
            _ = value.ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ObjectEquals_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public static class Program { public static void Main() { } }

            public abstract class ValueObject
            {
                public override bool Equals(object? obj)
                {
                    if (obj is null || obj.GetType() != GetType())
                    {
                        return false;
                    }

                    return true;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilder_AppendLine_AllStringParams()
    {
        var test = CreateTest();
        test.TestCode = """
            var sb = new System.Text.StringBuilder();
            var str = "";
            sb.AppendLine($"foo{str}var{str}");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilder_AppendLine_AllStringParams_Net7()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            using System;
            var sb = new System.Text.StringBuilder();
            var str = "";
            sb.AppendLine($"foo{str}var{str}{'a'}{Guid.NewGuid()}");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilder_AppendLine_Int32Params_Net7()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            var sb = new System.Text.StringBuilder();
            int value = 0;
            [|sb.AppendLine($"foo{value}")|];
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("o")]
    [InlineData("O")]
    [InlineData("r")]
    [InlineData("R")]
    [InlineData("s")]
    [InlineData("u")]
    public Task StringBuilder_AppendLine_DateTime_InvariantFormat_Net7(string format)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = $$$""""
            {{{$$"""
                        var sb = new System.Text.StringBuilder();
                        System.DateTime value = default;
                        sb.AppendLine($"foo{value:{{format}}}");
                        """}}}
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilder_AppendLine_DateTime_Net7()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net70;
        test.TestCode = """
            var sb = new System.Text.StringBuilder();
            System.DateTime value = default;
            [|sb.AppendLine($"foo{value:yyyy}")|];
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullableInt32ToStringWithoutCultureInfo()
    {
        var test = CreateTest();
        test.TestCode = """
            int? i = -1;
            [|i.ToString()|];
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NullableInt32ToStringWithoutCultureInfo_DisabledConfig()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0011.consider_nullable_types", "false");
        test.TestCode = """
            ((int?)1).ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveTypeAttribute_Assembly()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime))]
            _ = new System.DateTime().ToString();
            _ = new System.DateTime().ToString("whatever");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveTypeAttribute_Assembly_Format()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "custom")]
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), "")]
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), null)]
            _ = new System.DateTime().ToString("custom");
            _ = new System.DateTime().ToString("");
            _ = [|new System.DateTime().ToString("dummy")|];
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveTypeAttribute_Assembly_Format_null1()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute(typeof(System.DateTime), null)]
            _ = [|new System.DateTime().ToString("dummy")|];
            _ = new System.DateTime().ToString(format: null);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CultureInsensitiveAttribute_Member()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = Sample.Value.ToString();
            _ = Sample.Field.ToString();
            _ = [|Sample.OtherValue.ToString()|];

            static class Sample
            {
                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                public static double Value => 0;

                [Meziantou.Analyzer.Annotations.CultureInsensitive]
                public static double Field;

                public static double OtherValue => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_IFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|new Sample().ToString()|];

            class Sample : System.IFormattable
            {
                public override string ToString() => throw null;
                public string ToString(string format, System.IFormatProvider formatProvider) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_ISpanFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|new Sample().ToString()|];

            class Sample : System.ISpanFormattable
            {
                public override string ToString() => throw null;
                public string ToString(string? format, System.IFormatProvider? formatProvider) => throw null;
                public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider formatProvider) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_IFormattable_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|new Sample().ToString()|];

            class Sample : System.IFormattable
            {
                public override string ToString() => throw null;
                public string ToString(string format, System.IFormatProvider formatProvider) => throw null;
            }
            """;
        test.FixedCode = """
            _ = new Sample().ToString(null, System.Globalization.CultureInfo.InvariantCulture);

            class Sample : System.IFormattable
            {
                public override string ToString() => throw null;
                public string ToString(string format, System.IFormatProvider formatProvider) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_WithIFormatProviderOverload_WithoutIFormattable()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = [|new Location().ToString()|];

            class Location
            {
                public override string ToString() => throw null;
                public string ToString(System.IFormatProvider formatProvider) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_CultureSensitiveFormat_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;

            [|A.Print($"{DateTime.Now:D}")|];

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_CultureInvariantFormat_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;

            A.Print($"{DateTime.Now:o}");

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_NoFormattableTypes_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;

            A.Print($"XXX");

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_MixedFormats_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;

            [|A.Print($"{DateTime.Now:o} | {DateTime.Now:D}")|];

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_CustomTypeWithAttribute_CultureInvariantFormat_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using Meziantou.Analyzer.Annotations;

            A.Print($"{new Bar():o}");

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }

            [CultureInsensitiveType(format: "o")]
            sealed class Bar : IFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_CustomTypeWithAttribute_CultureSensitiveFormat_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using Meziantou.Analyzer.Annotations;

            [|A.Print($"{new Bar():D}")|];

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
                public static void Print(IFormatProvider provider, ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }

            [CultureInsensitiveType(format: "o")]
            sealed class Bar : IFormattable
            {
                public string ToString(string? format, IFormatProvider? formatProvider) => string.Empty;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringHandler_NoOverload_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Runtime.CompilerServices;

            A.Print($"{DateTime.Now:D}");

            class A
            {
                public static void Print(ref DefaultInterpolatedStringHandler interpolatedStringHandler) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_CultureSensitiveFormat_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            [|A.Sample($"{DateTime.Now:D}")|];

            class A
            {
                public static void Sample(FormattableString value) => throw null;
                public static void Sample(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_CultureInvariantFormat_ShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            A.Sample($"{DateTime.Now:o}");

            class A
            {
                public static void Sample(FormattableString value) => throw null;
                public static void Sample(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_Object_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(object value)
                {
                    Formatter.Print($"Value: {value}");
                }
            }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_Object_TreatOpaqueRuntimeTypesAsCultureSensitive_ShouldReport()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0011.treat_opaque_runtime_types_as_culture_sensitive", "true");
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(object value)
                {
                    [|Formatter.Print($"Value: {value}")|];
                }
            }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_Interface_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(IValue value)
                {
                    Formatter.Print($"Value: {value}");
                }
            }

            interface IValue { }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_IFormattable_ShouldReport()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(IFormattable value)
                {
                    [|Formatter.Print($"Value: {value}")|];
                }
            }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_NonSealedType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(Value value)
                {
                    Formatter.Print($"Value: {value}");
                }
            }

            class Value { }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_NonSealedType_TreatUnsealedTypesAsCultureSensitive_ShouldReport()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0011.treat_unsealed_types_as_culture_sensitive", "true");
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(Value value)
                {
                    [|Formatter.Print($"Value: {value}")|];
                }
            }

            class Value { }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_SealedNonFormattableType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A(Value value)
                {
                    Formatter.Print($"Value: {value}");
                }
            }

            sealed class Value
            {
                public override string ToString() => string.Empty;
            }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_UnconstrainedTypeParameter_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            public static class Program
            {
                public static void Main() { }
            }

            class Test
            {
                void A<T>(T value)
                {
                    Formatter.Print($"Value: {value}");
                }
            }

            static class Formatter
            {
                public static void Print(FormattableString value) => throw null;
                public static void Print(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_IFormatProviderNotLast_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            [|A.Sample($"{DateTime.Now:D}")|];

            class A
            {
                public static void Sample(FormattableString value) => throw null;
                public static void Sample(IFormatProvider format, FormattableString value) => throw null;
            }
            """;
        test.FixedCode = """
            using System;

            A.Sample(System.Globalization.CultureInfo.InvariantCulture, $"{DateTime.Now:D}");

            class A
            {
                public static void Sample(FormattableString value) => throw null;
                public static void Sample(IFormatProvider format, FormattableString value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_OptionalParameterBeforeIFormatProvider_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            [|A.Sample("prefix", $"{DateTime.Now:D}")|];

            class A
            {
                public static void Sample(string arg1, FormattableString value) => throw null;
                public static void Sample(string arg1, FormattableString value, int optionalParameter = 0, IFormatProvider format = null) => throw null;
            }
            """;
        test.FixedCode = """
            using System;

            A.Sample("prefix", $"{DateTime.Now:D}", format: System.Globalization.CultureInfo.InvariantCulture);

            class A
            {
                public static void Sample(string arg1, FormattableString value) => throw null;
                public static void Sample(string arg1, FormattableString value, int optionalParameter = 0, IFormatProvider format = null) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Enum_ParseWithoutFormatProvider_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            var color = System.Enum.Parse(typeof(Color), "Red");

            enum Color { Red, Green, Blue }
            """;

        return test.RunAsync();
    }
}
