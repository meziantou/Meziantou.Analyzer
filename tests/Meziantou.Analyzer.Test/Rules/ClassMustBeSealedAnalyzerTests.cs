using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ClassMustBeSealedAnalyzer,
    Meziantou.Analyzer.Rules.ClassMustBeSealedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ClassMustBeSealedAnalyzerTests
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
    public Task Inherited_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
            }

            class {|MA0053:Test2|} : Test
            {
            }
            """;
        test.FixedCode = """
            class Test
            {
            }

            sealed class Test2 : Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplementInterface_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            class {|MA0053:Test|} : ITest
            {
            }
            """;
        test.FixedCode = """
            interface ITest
            {
            }

            sealed class Test : ITest
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMethodAndConstField_NotReported()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                const int a = 10;
                static void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticMethodAndConstFieldWithEditorConfigTrue_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0053.public_class_should_be_sealed", "true");
        test.TestCode = """
            public class {|MA0053:Test|}
            {
                const int a = 10;
                static void A() { }
            }
            """;
        test.FixedCode = """
            public sealed class Test
            {
                const int a = 10;
                static void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericBaseClass()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Base<T>
            {
            }

            internal sealed class Child : Base<int>
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exception()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class SampleException : System.Exception
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Exception_ConfigEnabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0053.exceptions_should_be_sealed", "true");
        test.TestCode = """
            internal class {|MA0053:SampleException|} : System.Exception
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VirtualMember()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class SampleException
            {
                protected virtual void A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VirtualMember_EditorConfig()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0053.class_with_virtual_member_shoud_be_sealed", "true");
        test.TestCode = """
            internal class {|MA0053:SampleException|}
            {
                protected virtual void A() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ComImport()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Runtime.InteropServices.ComImport]
            [System.Runtime.InteropServices.Guid("1A894A19-2FCD-4F87-A5A2-83C64F9FA833")]
            internal class SampleException
            {
            }
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
    public Task BenchmarkDotNetAttributes()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("BenchmarkDotNet.Annotations", "0.13.2")]);
        test.TestCode = """
            using BenchmarkDotNet.Attributes;
            internal class Test
            {
                [Benchmark(Baseline = true)]
                public void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0053.class_with_virtual_member_shoud_be_sealed", "true");
        test.TestCode = """
            internal record {|MA0053:Sample|}();
            """;
        test.FixedCode = """
            internal sealed record Sample();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_Inherited_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            record Base();

            record {|MA0053:Derived|}() : Base();
            """;
        test.FixedCode = """
            record Base();

            sealed record Derived() : Base();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_ImplementInterface_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
            }

            record {|MA0053:Test|}() : ITest;
            """;
        test.FixedCode = """
            interface ITest
            {
            }

            sealed record Test() : ITest;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_Public_NotReported()
    {
        var test = CreateTest();
        test.TestCode = """
            public record Sample();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_Public_WithEditorConfig_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0053.public_class_should_be_sealed", "true");
        test.TestCode = """
            public record {|MA0053:Sample|}();
            """;
        test.FixedCode = """
            public sealed record Sample();
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("private protected")]
    public Task ClassWithPrivateCtor(string visibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class {|MA0053:Sample|}
            {
                {{visibility}} Sample() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("private protected")]
    public Task ClassWithMultiplePrivateCtors(string visibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class {|MA0053:Sample|}
            {
                private Sample(int a) { }
                {{visibility}} Sample() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    public Task ClassWithPublicCtor(string visibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Sample
            {
                {{visibility}} Sample() { }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    public Task ClassWithPrivateAndPublicCtor(string visibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            public class Sample
            {
                private Sample(int a) { }
                {{visibility}} Sample() { }
            }
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
}
