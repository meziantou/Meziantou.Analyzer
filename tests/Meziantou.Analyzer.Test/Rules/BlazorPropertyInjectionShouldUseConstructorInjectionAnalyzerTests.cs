using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzer,
    Meziantou.Analyzer.Rules.BlazorPropertyInjectionShouldUseConstructorInjectionFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class BlazorPropertyInjectionShouldUseConstructorInjectionAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest { ReferenceAssemblies = ReferenceAssemblies.Net.Net90.AddAspNetCore("9.0.0") };
        test.LanguageVersion = LanguageVersion.CSharp12;
        return test;
    }

    [Fact]
    public Task InjectProperty_IComponent_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : IComponent
            {
                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;

                public void Attach(RenderHandle renderHandle) { }
                public System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters) => System.Threading.Tasks.Task.CompletedTask;
            }
            """;
        test.FixedCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent(NavigationManager navigation) : IComponent
            {

                public void Attach(RenderHandle renderHandle) { }
                public System.Threading.Tasks.Task SetParametersAsync(ParameterView parameters) => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_ComponentBase_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;
            }
            """;
        test.FixedCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent(NavigationManager navigation) : ComponentBase
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_ExistingPrimaryConstructor_AddsParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.Extensions.Logging;

            class MyComponent(ILogger<MyComponent> logger) : ComponentBase
            {
                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;
            }
            """;
        test.FixedCode = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.Extensions.Logging;

            class MyComponent(ILogger<MyComponent> logger, NavigationManager navigation) : ComponentBase
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_WithExplicitConstructor_NoDiagnosticFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                public MyComponent() { }

                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoInjectAttribute_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                protected NavigationManager Navigation { get; set; } = default!;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_NotBlazorComponent_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class NotAComponent
            {
                [Inject]
                protected NavigationManager Navigation { get; set; } = default!;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_CSharp11_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp11;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddAspNetCore("8.0.0");
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                [Inject]
                protected NavigationManager Navigation { get; set; } = default!;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_AspNetCore8_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddAspNetCore("8.0.0");
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                [Inject]
                protected NavigationManager Navigation { get; set; } = default!;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InjectProperty_UpdatesUsages()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent : ComponentBase
            {
                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;

                private void HandleClick()
                {
                    Navigation.NavigateTo("/counter");
                }
            }
            """;
        test.FixedCode = """
            using Microsoft.AspNetCore.Components;

            class MyComponent(NavigationManager navigation) : ComponentBase
            {

                private void HandleClick()
                {
                    navigation.NavigateTo("/counter");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleInjectProperties_BatchFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            using Microsoft.Extensions.Logging;

            class MyComponent : ComponentBase
            {
                [Inject]
                protected NavigationManager {|MA0187:Navigation|} { get; set; } = default!;

                [Inject]
                protected ILogger<MyComponent> {|MA0187:Logger|} { get; set; } = default!;

                private void HandleClick()
                {
                    Navigation.NavigateTo("/counter");
                    Logger.LogInformation("Clicked");
                }
            }
            """;
        test.FixedCode = """
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
            """;

        return test.RunAsync();
    }
}
