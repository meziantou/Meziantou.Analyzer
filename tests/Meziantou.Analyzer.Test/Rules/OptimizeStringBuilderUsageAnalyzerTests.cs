using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeStringBuilderUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeStringBuilderUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeStringBuilderUsageAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task AppendFormat_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendFormat("{10}", 10);
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"new StringBuilder().AppendFormat(""NO PLACEHOLDERS"")")]
    [InlineData(@"new StringBuilder().AppendFormat(""NO PLACEHOLDERS"", true)")]
    [InlineData(@"new StringBuilder().AppendFormat(""NO PLACEHOLDERS"", true, false)")]
    [InlineData(@"new StringBuilder().AppendFormat(""NO PLACEHOLDERS"", true, false, 42)")]
    [InlineData(@"new StringBuilder().AppendFormat(null, ""NO PLACEHOLDERS"", true)")]
    public Task AppendFormat_NoPlaceholders_ReportDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$$""""
            {{{$$"""
                            using System.Text;
                            class Test
                            {
                                void A()
                                {
                                    {|MA0028:{{expression}}|};
                                }
                            }
                            """}}}
            """";

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"new StringBuilder().AppendFormat(""{0} {1}"", true, false)")]
    [InlineData(@"new StringBuilder().AppendFormat(null, ""{0} {1}"", true, false)")]
    public Task AppendFormat_WithPlaceholders_NoDiagnostic(string expression)
    {
        var test = CreateTest();
        test.TestCode = $$$""""
            {{{$$"""
                            using System.Text;
                            class Test
                            {
                                void A()
                                {
                                    {{expression}};
                                }
                            }
                            """}}}
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task AppendFormat_NoPlaceholders_FixReplacesWithAppend()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendFormat("NO PLACEHOLDERS", true)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append("NO PLACEHOLDERS");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendFormat_NoPlaceholders_WithProvider_FixReplacesWithAppend()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendFormat(null, "NO PLACEHOLDERS", true)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append("NO PLACEHOLDERS");
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("10")]
    [InlineData("10 + 20")]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    [InlineData(@"$""abc{""test""}""")]
    [InlineData(@"""abc"" + ""test""")]
    [InlineData(@"$""abc{""test""}"" + ""test""")]
    public Task Append_NoDiagnostic(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append({{text}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"""""")]
    [InlineData(@""""" + """"")]
    [InlineData(@""""".Substring(0, 10)")]
    public Task Append_ReportDiagnostic(string text)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Append({{text}})|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    public Task AppendLine_NoDiagnostic(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendLine({{text}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    [InlineData(@"$""{0}abc""")]

    public Task AppendLine_Net8_NoDiagnostic(string text)
    {
        var test = CreateTest();
        test.TestCode = $$$""""
            {{{$$"""
                            using System.Text;
                            class Test
                            {
                                void A()
                                {
                                    new StringBuilder().AppendLine({{text}});
                                }
                            }
                            """}}}
            """";

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"10.ToString()")]
    public Task AppendLine_ReportDiagnostic(string text)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine({{text}})|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"$""abc""")]
    [InlineData(@"$""a{1}""")]
    [InlineData(@"""a"" + 10")]
    [InlineData(@"10 + 20 + ""a""")]
    [InlineData(@"string.Format(""{0}"", 0)")]
    // StringBuilder.Insert has no counterpart for some of the Append overloads, so the ToString call is kept
    [InlineData(@"10.ToString()")]
    [InlineData(@"new StringBuilder().ToString()")]
    public Task Insert_NoDiagnostic(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Insert(0, {{text}});
                }
            }
            """;

        return test.RunAsync();
    }

    public static TheoryData<string> EmptyStringsArguments
    {
        get
        {
            return new TheoryData<string>
            {
                { @"$""""" },
                { @"$""{""""}""" },
                { @"""""" },
                { @""""" + """"" },
                { @"string.Empty" },
            };
        }
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public Task AppendLine_EmptyString(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine({{text}})|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public Task Append_EmptyString(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Append({{text}})|}.AppendLine();
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [MemberData(nameof(EmptyStringsArguments))]
    public Task Insert_EmptyString(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Insert(0, {{text}})|}.AppendLine();
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""a""")]
    public Task Append_OneCharString(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append({|MA0028:{{text}}|});
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append('a');
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""a""")]
    public Task Insert_OneCharString(string text)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Insert(0, {|MA0028:{{text}}|});
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Insert(0, 'a');
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_InterpolatedString()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Append($"A{1}BC{2:X2}DEF{1,-2:N2}")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append('A').Append(1).Append("BC").AppendFormat("{0:X2}", 2).Append("DEF").AppendFormat("{0,-2:N2}", 1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_InterpolatedString_FinishWithString()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine($"A{1}BC{2:X2}DEF")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append('A').Append(1).Append("BC").AppendFormat("{0:X2}", 2).AppendLine("DEF");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_InterpolatedString_FinishWithChar()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine($"A{1}BC{2:X2}D")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append('A').Append(1).Append("BC").AppendFormat("{0:X2}", 2).Append('D').AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_InterpolatedString_FinishWithObject()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine($"A{1}BC{2:X2}")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append('A').Append(1).Append("BC").AppendFormat("{0:X2}", 2).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_StringAdd()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    var a = "";
                    {|MA0028:new StringBuilder().Append("ab" + a)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    var a = "";
                    new StringBuilder().Append("ab").Append(a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_StringAdd()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    var a = "";
                    {|MA0028:new StringBuilder().AppendLine("ab" + a)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    var a = "";
                    new StringBuilder().Append("ab").AppendLine(a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_StringAdd_NonStringRightOperand()
    {
        var test = CreateTest();
        // Applying this fix reveals another diagnostic, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(int count)
                {
                    {|MA0028:new StringBuilder().AppendLine("a" + count)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(int count)
                {
                    new StringBuilder().Append({|MA0028:"a"|}).Append(count).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_StringAdd_NonStringRightOperand()
    {
        var test = CreateTest();
        // Applying this fix reveals another diagnostic, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(int count)
                {
                    {|MA0028:new StringBuilder().Append("a" + count)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(int count)
                {
                    new StringBuilder().Append({|MA0028:"a"|}).Append(count);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_StringAdd_NonStringLeftOperand()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(int count, string suffix)
                {
                    {|MA0028:new StringBuilder().AppendLine(count + suffix)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(int count, string suffix)
                {
                    new StringBuilder().Append(count).AppendLine(suffix);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_StringAdd_NullRightOperand()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(string prefix)
                {
                    {|MA0028:new StringBuilder().AppendLine(prefix + null)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(string prefix)
                {
                    new StringBuilder().Append(prefix).AppendLine(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_StringAdd_CharArrayOperand_NoCodeFix()
    {
        var test = CreateTest();
        // Applying this fix reveals another diagnostic, and the test asserts the result of a single application
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(char[] value)
                {
                    {|MA0028:new StringBuilder().AppendLine("a" + value)|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(char[] value)
                {
                    {|MA0028:new StringBuilder().AppendLine("a" + value)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Append(1.ToString())|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine(1.ToString())|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append(1).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_ToStringWithFormatAndCulture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append(1.ToString("N", null));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_AppendFormat_Variable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    new StringBuilder().Append(1.ToString(format, null));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_StringFormat_AppendFormat()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    {|MA0028:new StringBuilder().Append(string.Format("{0:N2}-{1:N0}", 1, 2))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    new StringBuilder().AppendFormat("{0:N2}-{1:N0}", 1, 2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_AppendFormat()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine(string.Format(null, "{0:N}", 1))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendFormat(null, "{0:N}", 1).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_AppendFormat_ImplicitParamsArray()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Globalization;
            using System.Text;

            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", string.Empty, "MilliSec", "%", "Comment"))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Globalization;
            using System.Text;

            class Test
            {
                void A()
                {
                    new StringBuilder().AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", string.Empty, "MilliSec", "%", "Comment").AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_StringJoin_AppendJoin_OldTargetFramework()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    new StringBuilder().Append(string.Join(", ", new[] { 1, 2, 3 }));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Append_StringJoin_AppendJoin()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    {|MA0028:new StringBuilder().Append(string.Join(", ", new[] { 1, 2, 3 }))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A(string format)
                {
                    new StringBuilder().AppendJoin(", ", new[] { 1, 2, 3 });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_AppendJoin()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine(string.Join(", ", new[] { 1, 2, 3 }))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().AppendJoin(", ", new[] { 1, 2, 3 }).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_AppendSubString()
    {
        var test = CreateTest();
        // Applying this fix reveals another diagnostic, and the test asserts the result of a single application
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne;
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine("".Substring(0, 1))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().Append("", 0, 1)|}.AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_AppendSubStringWithoutLength()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    {|MA0028:new StringBuilder().AppendLine("abc".Substring(2))|};
                }
            }
            """;
        test.FixedCode = """
            using System.Text;
            class Test
            {
                void A()
                {
                    new StringBuilder().Append("abc", 2, "abc".Length - 2).AppendLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AppendLine_CustomStructToString()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Text;
            struct MyStruct
            {
            }

            class Test
            {
                void A()
                {
                    new StringBuilder().AppendLine(new MyStruct().ToString());
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("System.ReadOnlySpan<char>")]
    [InlineData("System.ReadOnlyMemory<char>")]
    [InlineData("bool")]
    [InlineData("byte")]
    [InlineData("char")]
    [InlineData("char[]")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("short")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("sbyte")]
    [InlineData("float")]
    [InlineData("string")]
    [InlineData("System.Text.StringBuilder")]
    [InlineData("ushort")]
    [InlineData("uint")]
    [InlineData("ulong")]
    public Task AppendLine_ValueToString_Report(string dataType)
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = $$$""""
            {{{$$"""{|MA0028:new System.Text.StringBuilder().AppendLine(default({{dataType}}).ToString())|};"""}}}
            """";

        return test.RunAsync();
    }

    [Theory]
    [InlineData("object")]
    [InlineData("System.ReadOnlySpan<bool>")]
    public Task AppendLine_ValueToString_NoReport(string dataType)
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = $$$""""
            {{{$$"""new System.Text.StringBuilder().AppendLine(default({{dataType}}).ToString());"""}}}
            """";

        return test.RunAsync();
    }
}
