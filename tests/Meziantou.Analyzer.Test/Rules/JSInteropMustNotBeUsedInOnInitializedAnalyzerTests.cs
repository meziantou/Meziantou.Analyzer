using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class JSInteropMustNotBeUsedInOnInitializedAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<JSInteropMustNotBeUsedInOnInitializedAnalyzer>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task WebAssembly_NoReport()
    {
        await CreateProjectBuilder()
              .AddNuGetReference("Microsoft.JSInterop.WebAssembly", "6.0.10", "lib/net6.0/")
              .WithSourceCode("""
                  using System.Threading.Tasks;
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.JSInterop;
                  
                  class MyComponent : ComponentBase
                  {
                      public IJSRuntime JS { get; set; }
                      
                      protected override void OnInitialized()
                      {
                          _ = JS.InvokeVoidAsync("""
                  """;
    }

    protected override async Task OnInitializedAsync()
    {
        await JS.InvokeVoidAsync("""");
        await base.OnInitializedAsync();
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitialized_Report()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Threading.Tasks;
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.JSInterop;
                  class MyComponent : ComponentBase
                  {
                      public IJSRuntime JS { get; set; }
                      
                      protected override void OnInitialized()
                      {
                          _ = [||]JS.InvokeVoidAsync("""
                  """;
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitializedAsync_JsRuntimeExtensionMethod_Report()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Threading.Tasks;
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.JSInterop;
                  class MyComponent : ComponentBase
                  {
                      public IJSRuntime JS { get; set; }
                      
                      protected override async Task OnInitializedAsync()
                      {
                          await [||]JS.InvokeVoidAsync("""
                  """;
        await base.OnInitializedAsync();
    }
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitializedAsync_JsRuntimeInstance_Report()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Threading.Tasks;
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.JSInterop;
                  class MyComponent : ComponentBase
                  {
                      public IJSRuntime JS { get; set; }
                      
                      protected override async Task OnInitializedAsync()
                      {
                          await [||]JS.InvokeAsync<object>(identifier: """", args: new object[0]);
                          await base.OnInitializedAsync();
                      }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitializedAsync_ProtectedLocalStorage_Report()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  using System.Threading.Tasks;
                  using Microsoft.AspNetCore.Components;
                  using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
                  using Microsoft.JSInterop;
                  class MyComponent : ComponentBase
                  {
                      public ProtectedLocalStorage Storage { get; set; }
                      
                      protected override async Task OnInitializedAsync()
                      {
                          await [||]Storage.GetAsync<string>("""
                  """;
    }
}
")
              .ValidateAsync();
    }
}
