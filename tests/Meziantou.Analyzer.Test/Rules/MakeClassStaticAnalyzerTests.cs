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
            public class {|MA0036:Test|}
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
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTest();
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
            public sealed class {|MA0036:Test|}
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
            public partial class {|MA0036:Test|}
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
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            System.Console.WriteLine();
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("public System.Collections.Generic.List<Helper> Field;")]
    [InlineData("public Helper Field;")]
    [InlineData("public Helper[] Field;")]
    [InlineData("public (Helper, int) Field;")]
    [InlineData("public Helper Property { get; set; }")]
    [InlineData("public int this[Helper value] => 0;")]
    [InlineData("public event System.Action<Helper> Event;")]
    [InlineData("public void Method(Helper value) { }")]
    [InlineData("public Helper Method() => null;")]
    [InlineData("public void Method<T>() where T : Helper { }")]
    [InlineData("public void Method() { Helper value = null; }")]
    [InlineData("public void Method(object value) { foreach (Helper item in new object[0]) { } }")]
    [InlineData("public void Method(object value) { _ = value is Helper; }")]
    [InlineData("public void Method(object value) { _ = value is Helper helper; }")]
    [InlineData("public void Method(object value) { _ = value is Helper { }; }")]
    [InlineData("public void Method(object value) { _ = value switch { Helper => 1, _ => 0 }; }")]
    [InlineData("public void Method(object value) { _ = (Helper)value; }")]
    [InlineData("public void Method(object value) { _ = value as Helper; }")]
    [InlineData("public void Method() { _ = default(Helper); }")]
    [InlineData("public void Method() { _ = typeof(System.Collections.Generic.List<Helper>); }")]
    [InlineData("public void Method() { _ = typeof(Helper[]); }")]
    [InlineData("public void Method() { System.Delegate action = (Helper value) => { }; }")]
    [InlineData("public void Method() { void Local(Helper value) { } }")]
    [InlineData("public void Method() { _ = TryGet(out Helper value); } public static bool TryGet<T>(out T value) => throw null;")]
    [InlineData("public void Method() { _ = Generic<Helper>.Value; }")]
    [InlineData("public void Method() { Generic<Helper>.StaticMethod(); }")]
    [InlineData("public void Method() { System.Action action = Generic<Helper>.StaticMethod; }")]
    public Task TypeUsedInMember_NoDiagnostic(string member)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Helper
            {
                public static void M() { }
            }

            class Generic<T>
            {
                public static int Value;
                public int InstanceValue;
                public static void StaticMethod() { }
            }

            class Consumer
            {
                public int InstanceValue;
                {{member}}
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("delegate void Callback(Helper value);")]
    [InlineData("class Constrained<T> where T : Helper { public int InstanceValue; }")]
    [InlineData("class Outer<T> { public class Inner { public int InstanceValue; } public int InstanceValue; } class Consumer { public Outer<Helper>.Inner Field; }")]
    public Task TypeUsedInTypeDeclaration_NoDiagnostic(string declaration)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class Helper
            {
                public static void M() { }
            }

            {{declaration}}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TypeUsedAsTypeOfOperandOrStaticMemberAccess_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class {|MA0036:Helper|}
            {
                public static void M() { }
            }

            class Consumer
            {
                public int InstanceValue;

                public void Method()
                {
                    _ = typeof(Helper);
                    _ = nameof(Helper);
                    Helper.M();
                }
            }
            """;

        return test.RunAsync();
    }
}
