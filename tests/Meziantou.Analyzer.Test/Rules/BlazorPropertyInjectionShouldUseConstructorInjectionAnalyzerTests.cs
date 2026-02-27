using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzer>()
            .WithCodeFixProvider<BlazorPropertyInjectionShouldUseConstructorInjectionFixer>()
            .WithTargetFramework(TargetFramework.AspNetCore9_0)
            .WithLanguageVersion(LanguageVersion.CSharp12);
    }

    [Fact]
    public async Task InjectProperty_IComponent_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : IComponent
{
    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;

    public void Attach(RenderHandle renderHandle) { }
    public System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters) => System.Threading.Tasks.Task.CompletedTask;
}
""")
              .ShouldFixCodeWith("""
using Microsoft.AspNetCore.Components;

class MyComponent(NavigationManager navigation) : IComponent
{

    public void Attach(RenderHandle renderHandle) { }
    public System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters) => System.Threading.Tasks.Task.CompletedTask;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_ComponentBase_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;
}
""")
              .ShouldFixCodeWith("""
using Microsoft.AspNetCore.Components;

class MyComponent(NavigationManager navigation) : ComponentBase
{
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_ExistingPrimaryConstructor_AddsParameter()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

class MyComponent(ILogger<MyComponent> logger) : ComponentBase
{
    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;
}
""")
              .ShouldFixCodeWith("""
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

class MyComponent(ILogger<MyComponent> logger, NavigationManager navigation) : ComponentBase
{
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_WithExplicitConstructor_NoDiagnosticFix()
    {
        // Analyzer still reports, but no code fix when explicit non-primary constructor exists
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    public MyComponent() { }

    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task NoInjectAttribute_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    protected NavigationManager Navigation { get; set; } = default!;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_NotBlazorComponent_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class NotAComponent
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_CSharp11_NoDiagnostic()
    {
        await new ProjectBuilder()
              .WithAnalyzer<BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzer>()
              .WithTargetFramework(TargetFramework.AspNetCore9_0)
              .WithLanguageVersion(LanguageVersion.CSharp11)
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_AspNetCore8_NoDiagnostic()
    {
        await new ProjectBuilder()
              .WithAnalyzer<BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzer>()
              .WithTargetFramework(TargetFramework.AspNetCore8_0)
              .WithLanguageVersion(LanguageVersion.CSharp12)
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task InjectProperty_UpdatesUsages()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;

class MyComponent : ComponentBase
{
    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;

    private void HandleClick()
    {
        Navigation.NavigateTo("/counter");
    }
}
""")
              .ShouldFixCodeWith("""
using Microsoft.AspNetCore.Components;

class MyComponent(NavigationManager navigation) : ComponentBase
{

    private void HandleClick()
    {
        navigation.NavigateTo("/counter");
    }
}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleInjectProperties_BatchFix()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

class MyComponent : ComponentBase
{
    [Inject]
    protected NavigationManager [||]Navigation { get; set; } = default!;

    [Inject]
    protected ILogger<MyComponent> [||]Logger { get; set; } = default!;

    private void HandleClick()
    {
        Navigation.NavigateTo("/counter");
        Logger.LogInformation("Clicked");
    }
}
""")
              .ShouldBatchFixCodeWith("""
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

class MyComponent(NavigationManager navigation, ILogger<MyComponent> logger) : ComponentBase
{

    private void HandleClick()
    {
        navigation.NavigateTo("/counter");
        logger.LogInformation("Clicked");
    }
}
""")
              .ValidateAsync();
    }
}
