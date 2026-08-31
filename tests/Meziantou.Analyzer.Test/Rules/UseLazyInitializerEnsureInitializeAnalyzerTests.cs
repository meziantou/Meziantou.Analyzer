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
}
