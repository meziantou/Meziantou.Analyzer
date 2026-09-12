using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
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
    public Task WhenAvailable_NestedPropOfNestedProp()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(Sample foo)
                {
                    {|MA0166:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero)|};
                }

                class Sample { public Nested B {get;} }
                class Nested { public System.TimeProvider A {get;} }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(Sample foo)
                {
                    System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo.B.A);
                }

                class Sample { public Nested B {get;} }
                class Nested { public System.TimeProvider A {get;} }
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
    [Fact]
    public Task NamedArguments_OutOfOrder()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => {|MA0166:System.Threading.Tasks.Task.Delay(cancellationToken: token, delay: System.TimeSpan.Zero)|};
            }
            """;
        test.FixedCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => System.Threading.Tasks.Task.Delay(cancellationToken: token, delay: System.TimeSpan.Zero, timeProvider: foo);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NamedArguments_InOrder()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => {|MA0166:System.Threading.Tasks.Task.Delay(delay: System.TimeSpan.Zero, cancellationToken: token)|};
            }
            """;
        test.FixedCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => System.Threading.Tasks.Task.Delay(delay: System.TimeSpan.Zero, cancellationToken: token, timeProvider: foo);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NamedArgumentAfterTimeProviderParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => {|MA0166:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, cancellationToken: token)|};
            }
            """;
        test.FixedCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo, cancellationToken: token);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PositionalArgumentAfterTimeProviderParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => {|MA0166:System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, token)|};
            }
            """;
        test.FixedCode = """
            class Test
            {
                System.Threading.Tasks.Task A(System.TimeProvider foo, System.Threading.CancellationToken token)
                    => System.Threading.Tasks.Task.Delay(System.TimeSpan.Zero, foo, token);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptionalParameter_WithNamedArgumentBeforeTimeProvider()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void Delay(int a = 0, int b = 0, System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    {|MA0166:Delay(b: 1)|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                static void Delay(int a = 0, int b = 0, System.TimeProvider timeProvider = null)
                {
                }

                void A(System.TimeProvider foo)
                {
                    Delay(b: 1, timeProvider: foo);
                }
            }
            """;

        return test.RunAsync();
    }
    [Fact]
    public Task ExtensionMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            static class Test
            {
                static void A(this string str, int a) { }
                static void A(this string str, int a, System.TimeProvider timeProvider) { }

                static void B(System.TimeProvider foo) => {|MA0166:"".A(0)|};
            }
            """;
        test.FixedCode = """
            static class Test
            {
                static void A(this string str, int a) { }
                static void A(this string str, int a, System.TimeProvider timeProvider) { }

                static void B(System.TimeProvider foo) => "".A(0, timeProvider: foo);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void Delay(params int[] values) { }
                static void Delay(System.TimeProvider timeProvider, params int[] values) { }

                void A(System.TimeProvider foo)
                {
                    {|MA0166:Delay(1, 2)|};
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                static void Delay(params int[] values) { }
                static void Delay(System.TimeProvider timeProvider, params int[] values) { }

                void A(System.TimeProvider foo)
                {
                    Delay(foo, 1, 2);
                }
            }
            """;

        return test.RunAsync();
    }
    [Fact]
    public Task NoFix_WhenTheProviderCannotBeAddedWithoutReorderingTheArguments()
    {
        var test = CreateTest();
        test.TestCode = """
            static class Test
            {
                static void A(this string str, int a) { }
                static void A(this string str, System.TimeProvider timeProvider, int a) { }

                static void B(System.TimeProvider foo) => {|MA0166:"".A(0)|};
            }
            """;
        test.FixedCode = """
            static class Test
            {
                static void A(this string str, int a) { }
                static void A(this string str, System.TimeProvider timeProvider, int a) { }

                static void B(System.TimeProvider foo) => {|MA0166:"".A(0)|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReportsTheAvailableTimeProvidersInTheMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(System.TimeProvider timeProvider)
                {
                    {|#0:Delay()|};
                }

                static void Delay() { }
                static void Delay(System.TimeProvider timeProvider) { }
            }
            """;
        test.FixedCode = """
            class Test
            {
                void A(System.TimeProvider timeProvider)
                {
                    Delay(timeProvider);
                }

                static void Delay() { }
                static void Delay(System.TimeProvider timeProvider) { }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0166", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload with a TimeProvider, available time providers: timeProvider"));

        return test.RunAsync();
    }
}
