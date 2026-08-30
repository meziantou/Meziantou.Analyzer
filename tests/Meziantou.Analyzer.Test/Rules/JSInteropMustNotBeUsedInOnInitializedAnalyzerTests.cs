using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.JSInteropMustNotBeUsedInOnInitializedAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class JSInteropMustNotBeUsedInOnInitializedAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        return test;
    }

    [Fact]
    public Task WebAssembly_NoReport()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([new PackageIdentity("Microsoft.JSInterop.WebAssembly", AnalyzerTestDefaults.DotNetVersion)]);
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.JSInterop;

            class MyComponent : ComponentBase
            {
                public IJSRuntime JS { get; set; }

                protected override void OnInitialized()
                {
                    _ = JS.InvokeVoidAsync("");
                }

                protected override async Task OnInitializedAsync()
                {
                    await JS.InvokeVoidAsync("");
                    await base.OnInitializedAsync();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitialized_Report()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.JSInterop;
            class MyComponent : ComponentBase
            {
                public IJSRuntime JS { get; set; }

                protected override void OnInitialized()
                {
                    _ = [|JS.InvokeVoidAsync("")|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitializedAsync_JsRuntimeExtensionMethod_Report()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.JSInterop;
            class MyComponent : ComponentBase
            {
                public IJSRuntime JS { get; set; }

                protected override async Task OnInitializedAsync()
                {
                    await [|JS.InvokeVoidAsync("")|];
                    await base.OnInitializedAsync();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitializedAsync_JsRuntimeInstance_Report()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.JSInterop;
            class MyComponent : ComponentBase
            {
                public IJSRuntime JS { get; set; }

                protected override async Task OnInitializedAsync()
                {
                    await [|JS.InvokeAsync<object>(identifier: "", args: new object[0])|];
                    await base.OnInitializedAsync();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitializedAsync_ProtectedLocalStorage_Report()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
            using Microsoft.JSInterop;
            class MyComponent : ComponentBase
            {
                public ProtectedLocalStorage Storage { get; set; }

                protected override async Task OnInitializedAsync()
                {
                    await [|Storage.GetAsync<string>("")|];
                }
            }
            """;

        return test.RunAsync();
    }
}
