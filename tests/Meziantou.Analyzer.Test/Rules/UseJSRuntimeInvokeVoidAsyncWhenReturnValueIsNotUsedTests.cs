using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedAnalyzer,
    Meziantou.Analyzer.Rules.UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseJSRuntimeInvokeVoidAsyncWhenReturnValueIsNotUsedTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        return test;
    }

    [Fact]
    public Task IJSRuntime_InvokeAsync_ReturnNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await {|MA0120:js.InvokeAsync<string>("dummy")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await js.InvokeVoidAsync("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSRuntime_InvokeAsyncExplicit_ReturnNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await {|MA0120:JSRuntimeExtensions.InvokeAsync<string>(js, "", System.Array.Empty<object>())|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await JSRuntimeExtensions.InvokeVoidAsync(js, "", System.Array.Empty<object>());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSRuntime_InvokeAsyncExplicitWithCancellationToken_ReturnNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await {|MA0120:js.InvokeAsync<string>("dummy", CancellationToken.None, new object?[1] { null })|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await js.InvokeVoidAsync("dummy", CancellationToken.None, new object?[1] { null });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSRuntime_InvokeAsync_ReturnAssigned()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    var a = await js.InvokeAsync<string>("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSRuntime_InvokeAsync_ReturnAsArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    System.Console.WriteLine(await js.InvokeAsync<string>("dummy"));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSRuntime_InvokeVoidAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSRuntime js = null;
                    await js.InvokeVoidAsync("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_InvokeVoidAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSInProcessRuntime js = null;
                    await js.InvokeVoidAsync("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_InvokeVoid()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                void A()
                {
                    IJSInProcessRuntime js = null;
                    js.InvokeVoid("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_InvokeAsync_ReturnNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSInProcessRuntime js = null;
                    await {|MA0120:js.InvokeAsync<string>("dummy")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSInProcessRuntime js = null;
                    await js.InvokeVoidAsync("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_Invoke_ReturnNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                void A()
                {
                    IJSInProcessRuntime js = null;
                    {|MA0120:js.Invoke<string>("dummy")|};
                }
            }
            """;
        test.FixedCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                void A()
                {
                    IJSInProcessRuntime js = null;
                    js.InvokeVoid("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_InvokeAsync_ReturnAssigned()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                async Task A()
                {
                    IJSInProcessRuntime js = null;
                    var a = await js.InvokeAsync<string>("dummy");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IJSInProcessRuntime_Invoke_ReturnAssigned()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            class Sample
            {
                void A()
                {
                    IJSInProcessRuntime js = null;
                    var a = js.Invoke<string>("dummy");
                }
            }
            """;

        return test.RunAsync();
    }
}
