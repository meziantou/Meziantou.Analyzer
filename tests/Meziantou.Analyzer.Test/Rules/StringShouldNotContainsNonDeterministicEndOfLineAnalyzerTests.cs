using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.StringShouldNotContainsNonDeterministicEndOfLineAnalyzer,
    Meziantou.Analyzer.Rules.StringShouldNotContainsNonDeterministicEndOfLineFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class StringShouldNotContainsNonDeterministicEndOfLineAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        return test;
    }


    [Fact]
    public Task Valid()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "test";
                    _ = $"test";
                    _ = "test\r\nabc";
                    _ = $"test{0}\r\nabc";
                    _ = @"test";
                    _ = $@"test{0}";
                    _ = $@"test{
            0}";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0101:@"line1
            line2"|};
                }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "line1\n" +
                        "line2";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimString2()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0101:@"line1""\t
            line2"|};
                }
            }
            """;
        test.CodeActionIndex = 2;
        test.CodeFixTestBehaviors |= CodeFixTestBehaviors.FixOne | CodeFixTestBehaviors.SkipFixAllCheck;
        test.FixedCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "line1\"\\t\r\n" +
                        "line2";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimInterpolatedString()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0101:$@"line1{0}
            line2"|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task U8String()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = "line1"u8;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VerbatimU8String()
    {
        var test = CreateTest();
        test.TestCode = """
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0101:@"line1
                    line2"u8|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task U8RawString()
    {
        var test = CreateTest();
        test.TestCode = """"
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0136:"""
                        line1
                        line2
                        """u8|};
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task SingleLineRawString1()
    {
        var test = CreateTest();
        test.TestCode = """"
            class Dummy
            {
                void Test()
                {
                    _ = """
                    line1
                    """;
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task SingleLineRawString2()
    {
        var test = CreateTest();
        test.TestCode = """"
            class Dummy
            {
                void Test()
                {
                    _ = """line1""";
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task RawString()
    {
        var test = CreateTest();
        test.TestCode = """"
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0136:"""
                    line1
                    line2
                    """|};
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedRawString()
    {
        var test = CreateTest();
        test.TestCode = """"
            class Dummy
            {
                void Test()
                {
                    _ = {|MA0136:$"""
                    line1{0}
                    line2
                    """|};
                }
            }
            """";

        return test.RunAsync();
    }
}
