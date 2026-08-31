using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.LocalVariablesShouldNotHideSymbolsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class LocalVariablesShouldNotHideSymbolsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task LocalVariableHideField()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string a;

                void A()
                {
                    var {|MA0084:a|} = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableHideProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string Prop {get;set;}

                void A()
                {
                    var {|MA0084:Prop|} = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableHideVisibleFieldFromParentClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class Base
            {
                protected string a;
            }

            class Test : Base
            {
                void A()
                {
                    var {|MA0084:a|} = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableHidePrimaryConstructorParameter()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class Test(int a)
            {
                void A()
                {
                    var {|MA0084:a|} = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableDoesNotHidePrimaryConstructorParameterInStaticMethod()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class Test(int a)
            {
                static void A()
                {
                    var a = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableDoesNotHidePrimaryConstructorParameter()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            class Test(int a)
            {
                void A()
                {
                    var b = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableHideNotVisibleFieldFromParentClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class Base
            {
                private string a;
            }

            class Test : Base
            {
                void A()
                {
                    var a = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableDoesNotHideSymbol()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = 10;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableInLocalFunctionHidesLocalVariableFromContainingMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = 10;

                    void LocalFunction()
                    {
                        var {|MA0084:a|} = 10;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableInStaticLocalFunctionDoesNotHideLocalVariableFromContainingMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = 10;

                    static void LocalFunction()
                    {
                        var a = 10;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariableInNestedLocalFunctionDoesNotHideLocalVariableAcrossStaticLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = 10;

                    static void LocalFunction()
                    {
                        void NestedLocalFunction()
                        {
                            var a = 10;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}
