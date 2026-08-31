using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MakeClassStaticAnalyzer,
    Meziantou.Analyzer.Rules.MakeClassStaticFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MakeClassStaticAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();

        // The rule is reported by a compilation action, so the diagnostic is not local to the syntax tree,
        // which the testing library rejects for a code fix by default
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task AbstractClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class AbstractClass
            {
                static void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Inherited_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void A() { }
            }

            class Test2 : Test { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InstanceField_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test4
            {
                int _a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplementInterface_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test : ITest
            {
            }

            interface ITest { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMethodAndConstField_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class [|Test|]
            {
                const int a = 10;
                static void A() { }
            }
            """;
        test.FixedCode = """
            public static class Test
            {
                const int a = 10;
                static void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConversionOperator_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static implicit operator int(Test _) => 1;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AddOperator_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static Test operator +(Test a, Test b) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComImport_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Runtime.InteropServices.CoClass(typeof(Test))]
            interface ITest
            {
            }

            class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Instantiation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public static void A() => new Test();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MsTestClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTestApi();
        test.TestCode = """
            [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
            class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class [|Test|]
            {
            }
            """;
        test.FixedCode = """
            public static class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericClass_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void A<T>() => throw null;
                static void B() => A<Test>();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Array_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void A() => _ = new Test[0];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericObjectCreation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void A() => new Test2<Test>();
            }

            class Test2<T>
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericInvocation_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static void A() => Test2.A<Test>();
            }

            static class Test2
            {
                public static void A<T>() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FixShouldAddStaticBeforePartial()
    {
        var test = CreateTest();
        test.TestCode = """
            public partial class [|Test|]
            {
            }
            """;
        test.FixedCode = """
            public static partial class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericType()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            public class Query : IQuery<Result<QueryResult>> { }
            public sealed record QueryResult();
            public interface IQuery<T> { }
            public class Result<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement_9()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement_10()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp10;
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            """;

        return test.RunAsync();
    }
}
