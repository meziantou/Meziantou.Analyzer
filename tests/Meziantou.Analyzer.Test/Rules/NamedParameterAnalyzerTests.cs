using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.NamedParameterAnalyzer,
    Meziantou.Analyzer.Rules.NamedParameterFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NamedParameterAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }

    [Fact]

    public Task MethodWithNoParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                TypeName() { }
                void A() { }
                int B => 0;

                public void Test()
                {
                    _ = new TypeName();
                    A();
                    _ = B;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]

    public Task Task_ConfigureAwait_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public async System.Threading.Tasks.Task Test()
                {
                    await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Task_T_ConfigureAwait_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public async System.Threading.Tasks.Task Test()
                {
                    await System.Threading.Tasks.Task.Run(() => 10).ConfigureAwait(true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Volatile_ReadWrite_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                private bool _value;

                public void Test()
                {
                    System.Threading.Volatile.Write(ref _value, false);
                    _ = System.Threading.Volatile.Read(ref _value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NamedParameter_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(objA: true, "");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task True_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("", "", [|true|]);
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("", "", ignoreCase: true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BatchFix_MultipleArgumentsInSingleInvocation()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                private void InsertStatus(object reviewStatuses, object courseStatuses, object paymentInfos, object utcStatuses, object dmvStatuses)
                {
                }

                public void Test()
                {
                    this.InsertStatus([|null|], [|null|], [|null|], utcStatuses: null, [|null|]);
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                private void InsertStatus(object reviewStatuses, object courseStatuses, object paymentInfos, object utcStatuses, object dmvStatuses)
                {
                }

                public void Test()
                {
                    this.InsertStatus(reviewStatuses: null, courseStatuses: null, paymentInfos: null, utcStatuses: null, dmvStatuses: null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task True_WithOptions_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "None");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("", "", true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DefaultLiteral_WithoutOptions_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    A(default);
                    void A(object value) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DefaultLiteral_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "default");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    A([|default|]);
                    void A(object value) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "string, boolean");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [|""|],
                                    [|""|],
                                    [|true|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SingleLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "string, boolean");
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [|"""test"""|],
                                    [|"""test"""|],
                                    [|true|]);
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedStringLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "string, boolean");
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [|$"""test{0}"""|],
                                    [|"""test"""|],
                                    [|true|]);
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task MultiLinesRawString_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "string, boolean");
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [|"""
                                        test
                                        """|],
                                    [|"""
                                        test
                                        """|],
                                    [|true|]);
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedMultiLineLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "string, boolean");
        test.TestCode = """"
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [|$"""
                                        test{0}
                                        """|],
                                    [|"""
                                    test
                                    """|],
                                    [|true|]);
                }
            }
            """";

        return test.RunAsync();
    }

    [Fact]
    public Task Int32_WithOptions_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "numeric");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    A([|1|], [|1L|], [|3|]);
                    void A(int a, long b, short c) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Int32_WithOptions_ShouldNotReportDiagnosticForArrayIndexer()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "numeric");
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    int[] array = new[] {5, 4};

                    if (array[0] == 5)
                    {
                        array[0] = 6;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Int32_ExcludedMethod_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(("MA0003.expression_kinds", "numeric"), ("MA0003.excluded_methods_regex", "M[a-z][A-Z]ethod"));
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    MyMethod(1, 1L, 3);
                }

                void MyMethod(int a, long b, short c) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Int32_ExcludedMethodWithInvalidRegex_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration(("MA0003.expression_kinds", "numeric"), ("MA0003.excluded_methods_regex", "["));
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    MyMethod([|1|], [|1L|], [|3|]);
                }

                void MyMethod(int a, long b, short c) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task False_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(false, "");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Null_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(null, "");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodBaseInvoke_FirstArg_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("").Invoke(null, new object[0]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodBaseInvoke_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("").Invoke(null, [|null|]);
                }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("").Invoke(null, parameters: null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MSTestAssert_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTestApi();
        test.TestCode = """
            class TypeName
            {
                public void Test() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(null, true);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NunitAssert_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddNUnitApi();
        test.TestCode = """
            class TypeName
            {
                public void Test() => NUnit.Framework.Assert.AreEqual(null, true);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XunitAssert_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXUnitApi();
        test.TestCode = """
            class TypeName
            {
                public void Test() => Xunit.Assert.Equal(null, "dummy");
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor_ShouldUseTheRightParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    new TypeName([|null|]);
                }

                TypeName(string a) { }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    new TypeName(a: null);
                }

                TypeName(string a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitCtor_ShouldUseTheRightParameterName()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    TypeName a = new([|null|]);
                }

                TypeName(string a) { }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public void Test()
                {
                    TypeName a = new(a: null);
                }

                TypeName(string a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorChaining()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public TypeName()
                    : this([|null|])
                {
                }

                public TypeName(string a) { }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                public TypeName()
                    : this(a: null)
                {
                }

                public TypeName(string a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorBase()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseType
            {
                protected BaseType(string a) { }
            }
            class TypeName: BaseType
            {
                public TypeName()
                    : base([|null|])
                {
                }
            }
            """;
        test.FixedCode = """
            class BaseType
            {
                protected BaseType(string a) { }
            }
            class TypeName: BaseType
            {
                public TypeName()
                    : base(a: null)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyBuilder_IsUnicode_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public async System.Threading.Tasks.Task Test()
                {
                    new Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<int>().IsUnicode(false);
                }
            }

            namespace Microsoft.EntityFrameworkCore.Metadata.Builders
            {
                public class PropertyBuilder<TProperty>
                {
                    public bool IsUnicode(bool value) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Task_FromResult_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = System.Threading.Tasks.Task.FromResult(true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValueTask_FromResult_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        test.TestCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = System.Threading.Tasks.ValueTask.FromResult<System.ReadOnlyMemory<byte>?>(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Expression_IEnumerable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.Where(x => M([|false|]));
                }

                static bool M(bool a) => false;
            }

            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Expression_IQueryable_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IQueryable<string> query = null;
                    query.Where(x => M(false));
                }

                static bool M(bool a) => false;
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task Expression_ParamsInLambda_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp14;
        test.TestCode = """
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IQueryable<string> query = null;
                    query.Where(x => M([|false|]));
                }

                static bool M(bool a) => false;
            }
            """;

        return test.RunAsync();
    }
#endif

    [Fact]
    public Task Expression_ShouldNotReportDiagnostic2()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;

            class Test
            {
                public Test()
                {
                    Mock<ITest> mock = null;
                    mock.Setup(x => x.M(false));
                }

                static bool M(bool a) => false;
            }

            interface ITest
            {
                bool M(bool a);
            }

            class Mock<T>
            {
                public void Setup<TResult>(Expression<Func<T, TResult>> expression) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxNode_With()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = new Microsoft.CodeAnalysis.SyntaxNode();
                    _ = a.WithElse(null);
                }
            }

            namespace Microsoft.CodeAnalysis
            {
                public class SyntaxNode
                {
                    public SyntaxNode WithElse(object value) => throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SyntaxNode_EnablePrefix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    EnableTest(false);
                }

                void EnableTest(bool value) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task List_Add()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var list = new System.Collections.Generic.List<string>();
                    list.Add(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskCompletionSource_SetResult()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = new System.Threading.Tasks.TaskCompletionSource<string>();
                    a.SetResult(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Expression_Constant()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    _ = System.Linq.Expressions.Expression.Constant(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TaskCompletionSource_TrySetResult()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var a = new System.Threading.Tasks.TaskCompletionSource<string>();
                    _ = a.TrySetResult(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_Params()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    string.Format("Hi {0}, {1}, {2}, {3}.", null, null, null, null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_Array()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    string.Format("Hi {0}, {1}, {2}, {3}.", new object[] { null, null, null, null });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringFormat_Array_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    string.Format("Hi {0}, {1}, {2}, {3}.", (object[])null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Params_Array_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    B([|null|]);
                }

                void B(params int[] a) {}
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor_Params_Null()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public Test(params object[] a) { }

                void A()
                {
                    new Test(null, null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArrayIndexer()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var d = new[] {"Foo"};
                    if (d[0] == "X")
                    {
                        d[0] = "XXX";
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public int this[string value] => 0;

                void A()
                {
                    _ = this[null];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dictionary_Indexer()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var dict = new System.Collections.Generic.Dictionary<bool, object>();
                    dict[false] = null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_MultipleArgument()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.expression_kinds", "numeric");
        test.TestCode = """
            class Test
            {
                public int this[int x, int y] => 0;

                void A()
                {
                    _ = this[[|0|], [|0|]];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Tuples()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public Test(string a) { }

                void A()
                {
                    _ = (false, new Test([|null|]));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object a) { }

                void A()
                {
                    _ = new Test([|new object()|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_False()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(false)]object a) { }

                void A()
                {
                    _ = new Test(new object());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_True()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(true)]object a) { }

                void A()
                {
                    _ = new Test([|new object()|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ExtensionMethodReceiver_InstanceSyntax()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            static class Test
            {
                public static void A([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]this object value) { }

                static void B()
                {
                    new object().A();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ExtensionMethodReceiver_StaticSyntax()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            static class Test
            {
                public static void A([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]this object value) { }

                static void B()
                {
                    Test.A(new object());
                }
            }
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task CallerMustUseNamedArgument_ExtensionBlockReceiver()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            static class Test
            {
                extension([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object value)
                {
                    public void A() { }
                }

                static void B()
                {
                    new object().A();
                    Test.A(new object());
                }
            }
            """;

        return test.RunAsync();
    }
#endif

    [Fact]
    public Task CallerMustUseNamedArgument_ExtensionMethodNonReceiverParameter()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            static class Test
            {
                public static void A(this object value, [Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object other) { }

                static void B()
                {
                    new object().A([|new object()|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsLocalWithSameName()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                void A()
                {
                    object sample = null;
                    _ = new Test(sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsParameterWithSameNameIgnoringCase()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                void A(object Sample)
                {
                    _ = new Test(Sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsPropertyWithSameName()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                object Sample => null;

                void A()
                {
                    _ = new Test(this.Sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsFieldWithSameName()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                object sample;

                void A()
                {
                    _ = new Test(sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsFieldWithUnderscorePrefix()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                object _sample;

                void A()
                {
                    _ = new Test(_sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsStaticFieldWithUnderscorePrefix()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                static object s_sample;

                void A()
                {
                    _ = new Test(s_sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsLocalWithUnderscorePrefix()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                void A()
                {
                    object _sample = null;
                    _ = new Test([|_sample|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsFieldNamedLikeTheParameterWithUnderscorePrefix()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object _sample) { }

                object _sample;

                void A()
                {
                    _ = new Test(_sample);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsLocalWithDifferentName()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                void A()
                {
                    object other = null;
                    _ = new Test([|other|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CallerMustUseNamedArgument_ArgumentIsLocalWithSameName_OptionDisabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.ignore_arguments_matching_parameter_name", "false");
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object sample) { }

                void A()
                {
                    object sample = null;
                    _ = new Test([|sample|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MinimumNumberOfParameters_2_RequireNamedArgumentAttribute()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.minimum_method_parameters", "2");
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            class Test
            {
                public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(true)]object a) { }

                void A()
                {
                    _ = new Test([|new object()|]);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MinimumNumberOfParameters_2()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0003.minimum_method_parameters", "2");
        test.TestCode = """
            class Test
            {
                public Test(object a) { }

                void A()
                {
                    _ = new Test(new object());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task System_Action_1()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    System.Action<string> action = null;
                    action(null);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task System_Action_2()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    System.Action<string, string> action = null;
                    action(null, null);
                }
            }
            """;

        return test.RunAsync();
    }
}
