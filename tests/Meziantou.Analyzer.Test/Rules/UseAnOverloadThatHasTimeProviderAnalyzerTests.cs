using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasTimeProviderAnalyzer,
    Meziantou.Analyzer.Rules.UseAnOverloadThatHasTimeProviderFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseAnOverloadThatHasTimeProviderAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NoReport_ConsoleWriteLine()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine("test");
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_TimeSpanFromSeconds()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            _ = System.TimeSpan.FromSeconds(1);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotAvailable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0167:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoReport_WrongOverload()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    B();
                }

                void B() { }
                void B(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WhenAvailable_Parameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(System.TimeProvider foo)
                {
                    {|MA0166:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(System.TimeProvider foo)
                {
                    System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WhenAvailable_NestedProp()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(Sample foo)
                {
                    {|MA0166:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|};
                }

                class Sample { public System.TimeProvider A {get;} }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(Sample foo)
                {
                    System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo.A);
                }

                class Sample { public System.TimeProvider A {get;} }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionalParameter_WhenAvailable()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void Delay(System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    {|MA0166:Delay()|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                static void Delay(System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    Delay(foo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionalParameter_WithOptionalParameterBeforeTimeProvider()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void Delay(bool dummy = false, System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    {|MA0166:Delay()|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                static void Delay(bool dummy = false, System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    Delay(timeProvider: foo);
                }
            }
            """;

        return test.RunAsync();
    }
}
