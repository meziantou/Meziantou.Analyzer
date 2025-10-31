using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class NamedParameterAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<NamedParameterAnalyzer>()
            .WithCodeFixProvider<NamedParameterFixer>()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }

    [Fact]

    public async Task MethodWithNoParameter()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]

    public async Task Task_ConfigureAwait_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public async System.Threading.Tasks.Task Test()
                {
                    await System.Threading.Tasks.Task.Run(()=>{}).ConfigureAwait(false);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Task_T_ConfigureAwait_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public async System.Threading.Tasks.Task Test()
                {
                    await System.Threading.Tasks.Task.Run(() => 10).ConfigureAwait(true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NamedParameter_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(objA: true, """");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task True_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("""", """", [||]true);
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("""", """", ignoreCase: true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task True_WithOptions_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare("""", """", true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "None")
              .ValidateAsync();
    }

    [Fact]
    public async Task String_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [||]"""",
                                    [||]"""",
                                    [||]true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "string, boolean")
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task SingleLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """"""
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [||]"""test""",
                                    [||]"""test""",
                                    [||]true);
                }
            }
            """""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "string, boolean")
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedStringLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """"""
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [||]$"""test{0}""",
                                    [||]"""test""",
                                    [||]true);
                }
            }
            """""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "string, boolean")
              .ValidateAsync();
    }

    [Fact]
    public async Task MultiLinesRawString_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """"""
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [||]"""
                                        test
                                        """,
                                    [||]"""
                                        test
                                        """,
                                    [||]true);
                }
            }
            """""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "string, boolean")
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedMultiLineLineRawString_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """"""
            class TypeName
            {
                public void Test()
                {
                    var a = string.Compare(
                                    [||]$"""
                                        test{0}
                                        """,
                                    [||]"""
                                    test
                                    """,
                                    [||]true);
                }
            }
            """""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "string, boolean")
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task Int32_WithOptions_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    A([||]1, [||]1L, [||]3);
                    void A(int a, long b, short c) { }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "numeric")
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32_WithOptions_ShouldNotReportDiagnosticForArrayIndexer()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "numeric")
              .ValidateAsync();
    }

    [Fact]
    public async Task Int32_ExcludedMethod_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    MyMethod(1, 1L, 3);
                }
            
                void MyMethod(int a, long b, short c) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0003.excluded_methods_regex", "M[a-z][A-Z]ethod")
              .ValidateAsync();
    }

    [Fact]
    public async Task False_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(false, """");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Null_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    object.Equals(null, """");
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodBaseInvoke_FirstArg_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("""").Invoke(null, new object[0]);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MethodBaseInvoke_ShouldReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("""").Invoke(null, [||]null);
                }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    typeof(TypeName).GetMethod("""").Invoke(null, parameters: null);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task MSTestAssert_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(null, true);
            }
            """;
        await CreateProjectBuilder()
              .AddMSTestApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NunitAssert_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test() => NUnit.Framework.Assert.AreEqual(null, true);
            }
            """;
        await CreateProjectBuilder()
              .AddNUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task XunitAssert_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test() => Xunit.Assert.Equal(null, ""dummy"");
            }
            """;
        await CreateProjectBuilder()
              .AddXUnitApi()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor_ShouldUseTheRightParameterName()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    new TypeName([||]null);
                }
            
                TypeName(string a) { }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    new TypeName(a: null);
                }
            
                TypeName(string a) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitCtor_ShouldUseTheRightParameterName()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    TypeName a = new([||]null);
                }
            
                TypeName(string a) { }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public void Test()
                {
                    TypeName a = new(a: null);
                }
            
                TypeName(string a) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorChaining()
    {
        const string SourceCode = """
            class TypeName
            {
                public TypeName()
                    : this([||]null)
                {
                }
            
                public TypeName(string a) { }
            }
            """;
        const string CodeFix = """
            class TypeName
            {
                public TypeName()
                    : this(a: null)
                {
                }
            
                public TypeName(string a) { }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task CtorBase()
    {
        const string SourceCode = """
            class BaseType
            {
                protected BaseType(string a) { }
            }
            class TypeName: BaseType
            {
                public TypeName()
                    : base([||]null)
                {
                }
            }
            """;
        const string CodeFix = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyBuilder_IsUnicode_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Task_FromResult_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = System.Threading.Tasks.Task.FromResult(true);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ValueTask_FromResult_ShouldNotReportDiagnostic()
    {
        const string SourceCode = """
            class TypeName
            {
                public void Test()
                {
                    _ = System.Threading.Tasks.ValueTask.FromResult<System.ReadOnlyMemory<byte>?>(null);
                }
            }
            """;
        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Expression_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Linq;
using System.Collections.Generic;

class Test
{
    public Test()
    {
        IEnumerable<string> query = null;
        query.Where(x => M([||]false));
    }

    static bool M(bool a) => false;
}
")
              .ValidateAsync();

        await CreateProjectBuilder()
              .WithSourceCode(@"using System.Linq;
class Test
{
    public Test()
    {
        IQueryable<string> query = null;
        query.Where(x => M(false));
    }

    static bool M(bool a) => false;
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Expression_ShouldNotReportDiagnostic2()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
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
")
              .ValidateAsync();
    }

    [Fact]
    public async Task SyntaxNode_With()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
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
")
              .ValidateAsync();
    }

    [Fact]
    public async Task SyntaxNode_EnablePrefix()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        EnableTest(false);
    }

    void EnableTest(bool value) { }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task List_Add()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        var list = new System.Collections.Generic.List<string>();
        list.Add(null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskCompletionSource_SetResult()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        var a = new System.Threading.Tasks.TaskCompletionSource<string>();
        a.SetResult(null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Expression_Constant()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        _ = System.Linq.Expressions.Expression.Constant(null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task TaskCompletionSource_TrySetResult()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        var a = new System.Threading.Tasks.TaskCompletionSource<string>();
        _ = a.TrySetResult(null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_Params()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        string.Format(""Hi {0}, {1}, {2}, {3}."", null, null, null, null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_Array()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        string.Format(""Hi {0}, {1}, {2}, {3}."", new object[] { null, null, null, null });
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task StringFormat_Array_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        string.Format(""Hi {0}, {1}, {2}, {3}."", (object[])null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Params_Array_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        B([||]null);
    }

    void B(params int[] a) {}
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor_Params_Null()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public Test(params object[] a) { }

    void A()
    {
        new Test(null, null);
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task ArrayIndexer()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        var d = new[] {""Foo""};
        if (d[0] == ""X"")
        {
            d[0] = ""XXX"";
        }
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Indexer()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public int this[string value] => 0;

    void A()
    {
        _ = this[null];
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Dictionary_Indexer()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    void A()
    {
        var dict = new System.Collections.Generic.Dictionary<bool, object>();
        dict[false] = null;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task Indexer_MultipleArgument()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public int this[int x, int y] => 0;

    void A()
    {
        _ = this[[||]0, [||]0];
    }
}
")
              .AddAnalyzerConfiguration("MA0003.expression_kinds", "numeric")
              .ValidateAsync();
    }

    [Fact]
    public async Task Tuples()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public Test(string a) { }

    void A()
    {
        _ = (false, new Test([||]null));
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallerMustUseNamedArgument()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute]object a) { }

    void A()
    {
        _ = new Test([||]new object());
    }
}

namespace Meziantou.Analyzer.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Parameter)]
    internal class RequireNamedArgumentAttribute : System.Attribute {}
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallerMustUseNamedArgument_False()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(false)]object a) { }

    void A()
    {
        _ = new Test(new object());
    }
}

namespace Meziantou.Analyzer.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Parameter)]
    internal class RequireNamedArgumentAttribute : System.Attribute
    {
        public RequireNamedArgumentAttribute() {}
        public RequireNamedArgumentAttribute(bool value) {}
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task CallerMustUseNamedArgument_True()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test
{
    public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(true)]object a) { }

    void A()
    {
        _ = new Test([||]new object());
    }
}

namespace Meziantou.Analyzer.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Parameter)]
    internal class RequireNamedArgumentAttribute : System.Attribute
    {
        public RequireNamedArgumentAttribute() {}
        public RequireNamedArgumentAttribute(bool value) {}
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task MinimumNumberOfParameters_2_RequireNamedArgumentAttribute()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0003.minimum_method_parameters", "2")
              .WithSourceCode(@"
class Test
{
    public Test([Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute(true)]object a) { }

    void A()
    {
        _ = new Test([||]new object());
    }
}

namespace Meziantou.Analyzer.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Parameter)]
    internal class RequireNamedArgumentAttribute : System.Attribute
    {
        public RequireNamedArgumentAttribute() {}
        public RequireNamedArgumentAttribute(bool value) {}
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task MinimumNumberOfParameters_2()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0003.minimum_method_parameters", "2")
              .WithSourceCode(@"
class Test
{
    public Test(object a) { }

    void A()
    {
        _ = new Test(new object());
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task System_Action_1()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          System.Action<string> action = null;
                          action(null);
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task System_Action_2()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Test
                  {
                      void A()
                      {
                          System.Action<string, string> action = null;
                          action(null, null);
                      }
                  }
                  """)
              .ValidateAsync();
    }
}
