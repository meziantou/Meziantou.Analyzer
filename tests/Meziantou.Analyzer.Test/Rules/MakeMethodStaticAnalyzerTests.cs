using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MakeMethodStaticAnalyzer,
    Meziantou.Analyzer.Rules.MakeMethodStaticFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MakeMethodStaticAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();

        // MA0038 and MA0041 are reported by a compilation action, so the diagnostics are not local to the syntax tree,
        // which the testing library rejects for a code fix by default
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task ExpressionBody()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void {|MA0038:A|}() => throw null;
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                static void A() => throw null;
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
                void A() { _ = this.TestProperty; }

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
            using System.Linq;
            class TestClass
            {
                void A()
                {
                    _ = from item in new [] { 1, 2 }
                        where Test(item)
                        select item;
                }

                public virtual bool Test(int item) => 0 > 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceMethodInLinqQuery_Select_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TestClass
            {
                void A()
                {
                    _ = from item in new [] { 1, 2 }
                        select Test(item);
                }

                public virtual bool Test(int item) => 0 > 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceMethodInLinqQuery_From_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TestClass
            {
                void A()
                {
                    _ = from item in this.Test()
                        select item;
                }

                public virtual int[] Test() => new [] { 1, 2 };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessInstanceMethodInLinqQuery_Let_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TestClass
            {
                void A()
                {
                    _ = from item in new [] { 1, 2 }
                        let b = Test()
                        select b;
                }

                public virtual int[] Test() => new [] { 1, 2 };
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LinqQuery_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TestClass
            {
                void {|MA0038:A|}()
                {
                    _ = from item in new [] { 1, 2 }
                        select item;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AccessStaticMethodInLinqQuery_Let_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            class TestClass
            {
                void {|MA0038:A|}()
                {
                    _ = from item in new [] { 1, 2 }
                        let b = Test()
                        select b.ToString();
                }

                public static int[] Test() => new [] { 1, 2 };
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
                void {|MA0038:A|}() { _ = TestProperty; }

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
                void {|MA0038:A|}() { TestMethod(); }

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
                void {|MA0038:A|}() { _ = _a; }

                public static int _a;
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
                void A() { _ = _a; }

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
                public void A() { }
            }

            interface ITest
            {
                void A();
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
                void ITest.A() { }
            }

            interface ITest
            {
                void A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodImplementAGenericInterface()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass : ITest<int>
            {
                public int A() => 0;
            }

            interface ITest<T>
            {
                T A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodImplementAGenericInterfaceInAGenericClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass<T> : ITest<T>
            {
                public T A() => throw null;
            }

            interface ITest<T>
            {
                T A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MethodUseAnAnonymousObject()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void {|MA0038:A|}()
                {
                    var obj = new { Prop = 0 };
                    _ = obj.Prop;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreateInstance()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void {|MA0038:A|}()
                {
                    _ = new TestClass();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CreateInstanceOfAnotherType()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void {|MA0038:A|}()
                {
                    _ = new TestClass2();
                }
            }

            class TestClass2
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MSTest_TestMethod()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTest();
        test.TestCode = """
            class TestClass
            {
                [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
                void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MSTest_DataTestMethod()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddMSTest();
        test.TestCode = """
            class TestClass
            {
                [Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod]
                void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XUnit_TestMethod()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
            class TestClass
            {
                [Xunit.Fact]
                void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XUnit_TestMethodCustomAttribute()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddXunitV3();
        test.TestCode = """
            class TestClass
            {
                private class CustomFactAttribute : Xunit.FactAttribute
                {
                }

                [CustomFactAttribute]
                void A()
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AspNetCore_Startup()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Builder;
            using Microsoft.Extensions.DependencyInjection;

            public class Startup
            {
                public void ConfigureServices(IServiceCollection services)
                {
                }

                public void Configure(IApplicationBuilder app)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AspNetCore_Middleware_Convention_Invoke()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Http;

            public class CustomMiddleware
            {
                public CustomMiddleware(RequestDelegate next)
                {
                }

                public Task Invoke(HttpContext httpContext)
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AspNetCore_Middleware_Convention_Interface()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Http;

            public class CustomMiddleware : IMiddleware
            {
                public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AspNetCore_Middleware_Convention_ExplicitInterface()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Http;

            public class CustomMiddleware : IMiddleware
            {
                Task IMiddleware.InvokeAsync(HttpContext httpContext, RequestDelegate next)
                {
                    throw null;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AbstractMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                protected abstract void A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Test
            {
                partial void A();
            }

            partial class Test
            {
                partial void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XamlEventHandler_Add_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            #pragma checksum "..\..\MainWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "25B36A30BAFC7BB7D58C2E7472CEB827253914A46567E515A46D7429205241EB"

            partial class Test
            {
                event System.EventHandler<System.EventArgs> TestEvent;
                void Initialize()
                {
            #line 4 "App.xaml"
                    TestEvent += Handler;
                }
            }

            partial class Test
            {
                void Handler(object sender, System.EventArgs e) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task XamlEventHandler_Assignement_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            #pragma checksum "..\..\MainWindow.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "25B36A30BAFC7BB7D58C2E7472CEB827253914A46567E515A46D7429205241EB"

            partial class Test
            {
                event System.EventHandler<System.EventArgs> TestEvent;
                void Initialize()
                {
            #line 4 "App.xaml"
                    TestEvent = Handler;
                }
            }

            partial class Test
            {
                void Handler(object sender, System.EventArgs e) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_ExpressionBody()
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
    public Task Property_AccessInstanceProperty_NoDiagnostic()
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
    public Task Property_AutoProperty_NoDiagnostic()
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
    public Task Property_AccessStaticProperty()
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
    public Task Property_AccessStaticMethod()
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
    public Task Property_AccessStaticField()
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
    public Task Property_AccessInstanceField()
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
    public Task Property_ImplementAnInterface()
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
    public Task Property_ExplicitlyImplementAnInterface()
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
