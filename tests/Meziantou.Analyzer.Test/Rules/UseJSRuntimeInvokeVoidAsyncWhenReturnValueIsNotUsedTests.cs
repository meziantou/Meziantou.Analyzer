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
            .WithAnalyzer<UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedAnalyzer>()
            .WithCodeFixProvider<UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedFixer>()
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
              .ShouldFixCodeWith(@"
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
    public async Task IJSRuntime_InvokeAsyncExplicit_ReturnNotUsed()
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
        await [||]JSRuntimeExtensions.InvokeAsync<string>(js, """", System.Array.Empty<object>());
    }
}
")
              .ShouldFixCodeWith(@"
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        await JSRuntimeExtensions.InvokeVoidAsync(js, """", System.Array.Empty<object>());
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task IJSRuntime_InvokeAsyncExplicitWithCancellationToken_ReturnNotUsed()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        await [||]js.InvokeAsync<string>(""dummy"", CancellationToken.None, new object?[1] { null });
    }
}
")
              .ShouldFixCodeWith(@"
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

class Sample
{
    async Task A()
    {
        IJSRuntime js = null;
        await js.InvokeVoidAsync(""dummy"", CancellationToken.None, new object?[1] { null });
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
              .ShouldFixCodeWith(@"
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
              .ShouldFixCodeWith(@"
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
