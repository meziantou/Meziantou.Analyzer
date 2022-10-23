using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsed>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task IJSRuntime_InvokeAsync_ReturnNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        await [||]js.InvokeAsync<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task IJSRuntime_InvokeAsync_ReturnAssigned()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        var a = await js.InvokeAsync<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSRuntime_InvokeAsync_ReturnAsArgument()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        System.Console.WriteLine(await js.InvokeAsync<string>(""dummy""));
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSRuntime_InvokeVoidAsync()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        await js.InvokeVoidAsync(""dummy"");
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task IJSInProcessRuntime_InvokeVoidAsync()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSInProcessRuntime js = null;
        await js.InvokeVoidAsync(""dummy"");
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSInProcessRuntime_InvokeVoid()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    void A()
    {
        IJSInProcessRuntime js = null;
        js.InvokeVoid(""dummy"");
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSInProcessRuntime_InvokeAsync_ReturnNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSInProcessRuntime js = null;
        await [||]js.InvokeAsync<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }    
    
    [Fact]
    public async Task IJSInProcessRuntime_Invoke_ReturnNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    void A()
    {
        IJSInProcessRuntime js = null;
        [||]js.Invoke<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSInProcessRuntime_InvokeAsync_ReturnAssigned()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSInProcessRuntime js = null;
        var a = await js.InvokeAsync<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task IJSInProcessRuntime_Invoke_ReturnAssigned()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    void A()
    {
        IJSInProcessRuntime js = null;
        var a = js.Invoke<string>(""dummy"");
    }
}
")
              .ValidateAsync();
    }    
}
