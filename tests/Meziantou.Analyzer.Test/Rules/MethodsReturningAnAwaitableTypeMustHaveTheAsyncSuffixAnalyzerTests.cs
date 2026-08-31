using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzer,
    Meziantou.Analyzer.Rules.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    [Fact]
    public Task AsyncMethodWithSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
                class TypeName
                {
                    System.Threading.Tasks.Task TestAsync() => throw null;
                }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncMethodWithoutSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                System.Threading.Tasks.Task {|MA0137:Test|}() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncMethodWithoutSuffix_NonAwaitableTypeAttribute_TaskWrappedType_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(Result))]

            class TypeName
            {
                Task<Result> {|MA0137:Test|}() => throw null;
            }

            class Result { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncMethodWithoutSuffix_NonAwaitableTypeAttribute_OpenGenericTaskWrappedType_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using System.Threading.Tasks;
            [assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(Result<>))]

            class TypeName
            {
                Task<Result<int>> {|MA0137:Test|}() => throw null;
            }

            class Result<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VoidMethodWithSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void {|MA0138:TestAsync|}() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VoidMethodWithoutSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VoidLocalFunctionWithSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test()
                {
                    void {|MA0138:FooAsync|}() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VoidLocalFunctionWithoutSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test()
                {
                    void Foo() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitableLocalFunctionWithoutSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test()
                {
                    _ = Foo();
                    System.Threading.Tasks.Task {|MA0137:Foo|}() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AwaitableLocalFunctionWithSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test()
                {
                    System.Threading.Tasks.Task FooAsync() => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatement()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            await System.Threading.Tasks.Task.Yield();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EntryPoint()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            static class Program
            {
                static async System.Threading.Tasks.Task Main()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerableWithoutSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|#0:Foo|}() => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0156", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Method returning IAsyncEnumerable<T> must use the 'Async' suffix"));

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerableWithoutSuffix_CodeFix_AddsAsyncSuffix()
    {
        // MA0156 and MA0157 contradict each other, a project enables one of them, not both
        var test = CreateTest();
        test.DisabledDiagnostics.Add("MA0137");
        test.DisabledDiagnostics.Add("MA0157");
        test.TestCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|MA0156:Foo|}() => throw null;
                void Caller() { _ = Foo(); }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> FooAsync() => throw null;
                void Caller() { _ = FooAsync(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerableWithSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|#0:FooAsync|}() => throw null;
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0157", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Method returning IAsyncEnumerable<T> must not use the 'Async' suffix"));

        return test.RunAsync();
    }

    [Fact]
    public Task IAsyncEnumerableWithSuffix_CodeFix_RemovesAsyncSuffix()
    {
        // MA0156 and MA0157 contradict each other, a project enables one of them, not both
        var test = CreateTest();
        test.DisabledDiagnostics.Add("MA0137");
        test.DisabledDiagnostics.Add("MA0156");
        test.TestCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> {|MA0157:FooAsync|}() => throw null;
                void Caller() { _ = FooAsync(); }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                System.Collections.Generic.IAsyncEnumerable<int> Foo() => throw null;
                void Caller() { _ = Foo(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTestMethods()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXUnitApi();
        test.TestCode = """
            class TypeName
            {
                [Xunit.Fact]
                System.Threading.Tasks.Task Foo() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncMethodWithoutSuffix_CodeFix_AddsAsyncSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                System.Threading.Tasks.Task {|MA0137:Test|}() => throw null;
                void Caller() { _ = Test(); }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                System.Threading.Tasks.Task TestAsync() => throw null;
                void Caller() { _ = TestAsync(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodNotReturningAwaitableTypeWithSuffix_CodeFix_RemovesAsyncSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void {|MA0138:TestAsync|}() => throw null;
                void Caller() { TestAsync(); }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                void Test() => throw null;
                void Caller() { Test(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VoidLocalFunctionWithSuffix_CodeFix_RemovesAsyncSuffix()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void Test()
                {
                    void {|MA0138:FooAsync|}() => throw null;
                    FooAsync();
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                void Test()
                {
                    void Foo() => throw null;
                    Foo();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTestMethods_ExcludeTestMethodsTrue()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0137.exclude_test_methods", "true");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXUnitApi();
        test.TestCode = """
            class TypeName
            {
                [Xunit.Fact]
                System.Threading.Tasks.Task Foo() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IgnoreTestMethods_ExcludeTestMethodsFalse()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0137.exclude_test_methods", "false");
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXUnitApi();
        test.TestCode = """
            class TypeName
            {
                [Xunit.Fact]
                System.Threading.Tasks.Task {|MA0137:Foo|}() => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigureAwait_IsIgnored()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            class TypeName
            {
                ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) => Task.CompletedTask.ConfigureAwait(continueOnCapturedContext);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GetAwaiter_IsIgnored()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            class TypeName
            {
                TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task WithCancellation_IsIgnored()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            class TypeName
            {
                IAsyncEnumerable<int> WithCancellation(CancellationToken cancellationToken) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyReturningTask_IsIgnored()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            class TypeName
            {
                Task Task => Task.CompletedTask;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyReturningTask_ExcludePropertyAccessorsTrue()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0137.exclude_property_accessors", "true");
        test.TestCode = """
            using System.Threading.Tasks;
            class TypeName
            {
                Task Task => Task.CompletedTask;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyReturningTask_ExcludePropertyAccessorsFalse_Diagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0137.exclude_property_accessors", "false");
        test.TestCode = """
            using System.Threading.Tasks;
            class TypeName
            {
                Task Task
                {
                    {|MA0137:get|} => System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        return test.RunAsync();
    }
}
