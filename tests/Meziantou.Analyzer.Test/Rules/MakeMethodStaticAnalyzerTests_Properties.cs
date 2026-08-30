using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MakeMethodStaticAnalyzer,
    Meziantou.Analyzer.Rules.MakeMethodStaticFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MakeMethodStaticAnalyzerTests_Properties
{
    // This class covers MA0041 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add(RuleIdentifiers.MakeMethodStatic);

        // MA0041 is reported by a compilation action, so the diagnostic is not local to the syntax tree,
        // which the testing library rejects for a code fix by default
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task ExpressionBodyAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int {|MA0041:A|} => throw null;
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                static int A => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceProperty_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int A => TestProperty;

                public int TestProperty { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceMethodInLinqQuery_Where_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessStaticProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                public int {|MA0041:A|} => TestProperty;

                public static int TestProperty => 0;
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                public static int A => TestProperty;

                public static int TestProperty => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessStaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int {|MA0041:A|} => TestMethod();

                public static int TestMethod() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessStaticField()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int {|MA0041:A|} => _a;

                static int _a;
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                static int A => _a;

                static int _a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceField()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int A => _a;

                public int _a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodImplementAnInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass : ITest
            {
                public int A { get; }
            }

            interface ITest
            {
                int A { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodExplicitlyImplementAnInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass : ITest
            {
                int ITest.A { get; }
            }

            interface ITest
            {
                int A { get; }
            }
            """;

        return test.RunAsync();
    }
}
