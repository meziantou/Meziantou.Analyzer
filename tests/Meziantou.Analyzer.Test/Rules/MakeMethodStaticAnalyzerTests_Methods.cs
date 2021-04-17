using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class MakeMethodStaticAnalyzerTests_Methods
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MakeMethodStaticAnalyzer>(id: "MA0038")
                .WithCodeFixProvider<MakeMethodStaticFixer>();
        }

        [Fact]
        public async Task ExpressionBody()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A() => throw null;
}
";
            const string CodeFix = @"
class TestClass
{
    static void A() => throw null;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceProperty_NoDiagnostic()
        {
            const string SourceCode = @"
class TestClass
{
    void A() { _ = this.TestProperty; }

    public int TestProperty { get; }    
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceMethodInLinqQuery_Where_NoDiagnostic()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceMethodInLinqQuery_Select_NoDiagnostic()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceMethodInLinqQuery_From_NoDiagnostic()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceMethodInLinqQuery_Let_NoDiagnostic()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task LinqQuery_Diagnostic()
        {
            const string SourceCode = @"
using System.Linq;
class TestClass
{
    void [||]A()
    {
        _ = from item in new [] { 1, 2 }
            select item;
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessStaticMethodInLinqQuery_Let_Diagnostic()
        {
            const string SourceCode = @"
using System.Linq;
class TestClass
{
    void [||]A()
    {
        _ = from item in new [] { 1, 2 }
            let b = Test()
            select b.ToString();
    }

    public static int[] Test() => new [] { 1, 2 };
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessStaticProperty()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A() { _ = TestProperty; }

    public static int TestProperty => 0;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessStaticMethod()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A() { TestMethod(); }

    public static int TestMethod() => 0;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessStaticField()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A() { _ = _a; }

    public static int _a;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AccessInstanceField()
        {
            const string SourceCode = @"
class TestClass
{
    void A() { _ = _a; }

    public int _a;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodImplementAnInterface()
        {
            const string SourceCode = @"
class TestClass : ITest
{
    public void A() { }
}

interface ITest
{
    void A();
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodExplicitlyImplementAnInterface()
        {
            const string SourceCode = @"
class TestClass : ITest
{
    void ITest.A() { }
}

interface ITest
{
    void A();
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodImplementAGenericInterface()
        {
            const string SourceCode = @"
class TestClass : ITest<int>
{
    public int A() => 0;
}

interface ITest<T>
{
    T A();
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodImplementAGenericInterfaceInAGenericClass()
        {
            const string SourceCode = @"
class TestClass<T> : ITest<T>
{
    public T A() => throw null;
}

interface ITest<T>
{
    T A();
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MethodUseAnAnonymousObject()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A()
    {
        var obj = new { Prop = 0 };
        _ = obj.Prop;
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CreateInstance()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A()
    {
        _ = new TestClass();
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CreateInstanceOfAnotherType()
        {
            const string SourceCode = @"
class TestClass
{
    void [||]A()
    {
        _ = new TestClass2();
    }
}

class TestClass2
{
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MSTest_TestMethod()
        {
            const string SourceCode = @"
class TestClass
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
    void A()
    {
    }
}
";
            await CreateProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MSTest_DataTestMethod()
        {
            const string SourceCode = @"
class TestClass
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod]
    void A()
    {
    }
}
";
            await CreateProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task XUnit_TestMethod()
        {
            const string SourceCode = @"
class TestClass
{
    [Xunit.Fact]
    void A()
    {
    }
}
";
            await CreateProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task XUnit_TestMethodCustomAttribute()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AspNetCore_Startup()
        {
            const string SourceCode = @"
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
";
            await CreateProjectBuilder()
                  .WithTargetFramework(TargetFramework.AspNetCore5_0)
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AspNetCore_Middleware_Convention_Invoke()
        {
            const string SourceCode = @"
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
}";
            await CreateProjectBuilder()
                  .WithTargetFramework(TargetFramework.AspNetCore5_0)
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AspNetCore_Middleware_Convention_Interface()
        {
            const string SourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CustomMiddleware : IMiddleware
{
    public Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithTargetFramework(TargetFramework.AspNetCore5_0)
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AspNetCore_Middleware_Convention_ExplicitInterface()
        {
            const string SourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public class CustomMiddleware : IMiddleware
{
    Task IMiddleware.InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithTargetFramework(TargetFramework.AspNetCore5_0)
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AbstractMethod_NoDiagnostic()
        {
            const string SourceCode = @"
abstract class Test
{
    protected abstract void A();
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task PartialMethod_NoDiagnostic()
        {
            const string SourceCode = @"
partial class Test
{
    partial void A();
}

partial class Test
{
    partial void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task XamlEventHandler_Add_NoDiagnostic()
        {
            const string SourceCode = @"
#pragma checksum ""..\..\MainWindow.xaml"" ""{8829d00f-11b8-4213-878b-770e8597ac16}"" ""25B36A30BAFC7BB7D58C2E7472CEB827253914A46567E515A46D7429205241EB""

partial class Test
{
    event System.EventHandler<System.EventArgs> TestEvent;
    void Initialize()
    {
#line 4 ""App.xaml""
        TestEvent += Handler;
    }
}

partial class Test
{
    void Handler(object sender, System.EventArgs e) => throw null;
}

";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task XamlEventHandler_Assignement_NoDiagnostic()
        {
            const string SourceCode = @"
#pragma checksum ""..\..\MainWindow.xaml"" ""{8829d00f-11b8-4213-878b-770e8597ac16}"" ""25B36A30BAFC7BB7D58C2E7472CEB827253914A46567E515A46D7429205241EB""

partial class Test
{
    event System.EventHandler<System.EventArgs> TestEvent;
    void Initialize()
    {
#line 4 ""App.xaml""
        TestEvent = Handler;
    }
}

partial class Test
{
    void Handler(object sender, System.EventArgs e) => throw null;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
