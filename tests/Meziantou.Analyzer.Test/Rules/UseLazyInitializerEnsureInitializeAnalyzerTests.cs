using Microsoft.CodeAnalysis;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseLazyInitializerEnsureInitializeAnalyzer,
    Meziantou.Analyzer.Rules.UseLazyInitializerEnsureInitializeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseLazyInitializerEnsureInitializeAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task NewObject_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            object a = default;
            {|MA0173:System.Threading.Interlocked.CompareExchange(ref a, new object(), null)|};
            """;
        test.FixedCode = """
            object a = default;
            System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new object());
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewCustomClass_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = default;
            {|MA0173:System.Threading.Interlocked.CompareExchange(ref a, new Sample(), null)|};
            class Sample { };
            """;
        test.FixedCode = """
            Sample a = default;
            System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
            class Sample { };
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewCustomClass_Object_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            object? a = default;
            {|MA0173:System.Threading.Interlocked.CompareExchange(ref a, new Sample(), null)|};
            class Sample { };
            """;
        test.FixedCode = """
            object? a = default;
            System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
            class Sample { };
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewCustomClass_Default()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = default;
            {|MA0173:System.Threading.Interlocked.CompareExchange(ref a, new Sample(), default)|};
            class Sample { };
            """;
        test.FixedCode = """
            Sample a = default;
            System.Threading.LazyInitializer.EnsureInitialized(ref a, () => new Sample());
            class Sample { };
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_Field_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            _ = new Sample().M();

            class Sample
            {
                private static System.Func<string>? s_getDisplayName;

                public string M()
                {
                    System.Func<string> getDisplayName = () => string.Empty;
                    {|MA0173:System.Threading.Interlocked.CompareExchange(ref s_getDisplayName, getDisplayName, comparand: null)|};
                    return getDisplayName();
                }
            }
            """;
        test.FixedCode = """
            _ = new Sample().M();

            class Sample
            {
                private static System.Func<string>? s_getDisplayName;

                public string M()
                {
                    System.Func<string> getDisplayName = () => string.Empty;
                    System.Threading.LazyInitializer.EnsureInitialized(ref s_getDisplayName, () => getDisplayName);
                    return getDisplayName();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewCustomStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = default;
            System.Threading.Interlocked.CompareExchange(ref a, new Sample(), default);
            struct Sample { };
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NewInt32_Zero()
    {
        var test = CreateTest();
        test.TestCode = """
            int a = default;
            System.Threading.Interlocked.CompareExchange(ref a, 0, 0);
            """;

        return test.RunAsync();
    }
    [Fact]
    public Task Parameter()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample.Run(0);

            class Sample
            {
                private static object? s_target;

                public static void Run(int value)
                {
                    {|MA0173:System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(value), null)|};
                }
            }
            """;
        test.FixedCode = """
            Sample.Run(0);

            class Sample
            {
                private static object? s_target;

                public static void Run(int value)
                {
                    System.Threading.LazyInitializer.EnsureInitialized(ref s_target, () => new System.Text.StringBuilder(value));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            Sample.Run(ref value);

            class Sample
            {
                private static object? s_target;

                public static void Run(ref int value)
                {
                    System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(value), null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OutParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample.Run(out _);

            class Sample
            {
                private static object? s_target;

                public static void Run(out int value)
                {
                    value = 0;
                    System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(value), null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            var value = 0;
            Sample.Run(in value);

            class Sample
            {
                private static object? s_target;

                public static void Run(in int value)
                {
                    System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(value), null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefLocal()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample.Run();

            class Sample
            {
                private static object? s_target;
                private static int s_value;

                public static void Run()
                {
                    ref var value = ref s_value;
                    System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(value), null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefStructLocal()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample.Run();

            class Sample
            {
                private static object? s_target;

                public static void Run()
                {
                    System.Span<char> value = stackalloc char[1];
                    System.Threading.Interlocked.CompareExchange(ref s_target, new string(value), null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StructThis()
    {
        var test = CreateTest();
        test.TestCode = """
            default(Sample).Run();

            struct Sample
            {
                private static object? s_target;
                private int _value;

                public void Run()
                {
                    System.Threading.Interlocked.CompareExchange(ref s_target, new System.Text.StringBuilder(_value), null);
                }
            }
            """;

        return test.RunAsync();
    }

}
