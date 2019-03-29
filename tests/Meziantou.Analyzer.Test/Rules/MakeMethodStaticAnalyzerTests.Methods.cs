using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MakeMethodStaticAnalyzerTests_Methods : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new MakeMethodStaticAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new MakeMethodStaticFixer();
        protected override string ExpectedDiagnosticId => "MA0038";
        protected override string ExpectedDiagnosticMessage => "Make method static";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [TestMethod]
        public void ExpressionBody()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() => throw null;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));

            var fix = @"
class TestClass
{
    static void A() => throw null;
}
";

            VerifyFix(project, fix);
        }

        [TestMethod]
        public void AccessInstanceProperty_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() { _ = this.TestProperty; }

    public int TestProperty { get; }    
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_Where_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_Select_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_From_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AccessInstanceMethodInLinqQuery_Let_NoDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void LinqQuery_Diagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
using System.Linq;
class TestClass
{
    void A()
    {
        _ = from item in new [] { 1, 2 }
            select item;
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 10));
        }

        [TestMethod]
        public void AccessStaticMethodInLinqQuery_Let_Diagnostic()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(Enumerable))
                  .AddReference(typeof(IEnumerable<>))
                  .WithSourceCode(@"
using System.Linq;
class TestClass
{
    void A()
    {
        _ = from item in new [] { 1, 2 }
            let b = Test()
            select b.ToString();
    }

    public static int[] Test() => new [] { 1, 2 };
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 10));
        }

        [TestMethod]
        public void AccessStaticProperty()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() { _ = TestProperty; }

    public static int TestProperty => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void AccessStaticMethod()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() { TestMethod(); }

    public static int TestMethod() => 0;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void AccessStaticField()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() { _ = _a; }

    public static int _a;
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void AccessInstanceField()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A() { _ = _a; }

    public int _a;
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass : ITest
{
    public void A() { }
}

interface ITest
{
    void A();
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodExplicitlyImplementAnInterface()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass : ITest
{
    void ITest.A() { }
}

interface ITest
{
    void A();
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodImplementAGenericInterface()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass : ITest<int>
{
    public int A() => 0;
}

interface ITest<T>
{
    T A();
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodImplementAGenericInterfaceInAGenericClass()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass<T> : ITest<T>
{
    public T A() => throw null;
}

interface ITest<T>
{
    T A();
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MethodUseAnAnonymousObject()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A()
    {
        var obj = new { Prop = 0 };
        _ = obj.Prop;
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void CreateInstance()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A()
    {
        _ = new TestClass();
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void CreateInstanceOfAnotherType()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestClass
{
    void A()
    {
        _ = new TestClass2();
    }
}

class TestClass2
{
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 4, column: 10));
        }

        [TestMethod]
        public void MSTest_TestMethod()
        {
            var project = new ProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(@"
class TestClass
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
    void A()
    {
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MSTest_DataTestMethod()
        {
            var project = new ProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(@"
class TestClass
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod]
    void A()
    {
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void XUnit_TestMethod()
        {
            var project = new ProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(@"
class TestClass
{
    [Xunit.Fact]
    void A()
    {
    }
}
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AspNetCore_Startup()
        {
            var project = new ProjectBuilder()
                  .AddMicrosoftAspNetCoreApi()
                  .WithSourceCode(@"
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
");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AspNetCore_Middleware_Convention_Invoke()
        {
            var project = new ProjectBuilder()
                  .AddMicrosoftAspNetCoreApi()
                  .WithSourceCode(@"
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
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AspNetCore_Middleware_Convention_InvokeAsync()
        {
            var project = new ProjectBuilder()
                  .AddMicrosoftAspNetCoreApi()
                  .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CustomMiddleware
{
    public CustomMiddleware(RequestDelegate next)
    {
    }

    public Task InvokeAsync(HttpContext httpContext)
    {
        throw null;
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AspNetCore_Middleware_Convention_Interface()
        {
            var project = new ProjectBuilder()
                  .AddMicrosoftAspNetCoreApi()
                  .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CustomMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        throw null;
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AspNetCore_Middleware_Convention_ExplicitInterface()
        {
            var project = new ProjectBuilder()
                  .AddMicrosoftAspNetCoreApi()
                  .WithSourceCode(@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CustomMiddleware : IMiddleware
{
    Task IMiddleware.InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        throw null;
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AbstractMethod_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
abstract class Test
{
    protected abstract void A();
}");
            VerifyDiagnostic(project);
        }
    }
}
