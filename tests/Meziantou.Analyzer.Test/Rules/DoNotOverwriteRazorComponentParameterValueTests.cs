using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotOverwriteRazorComponentParameterValue>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotOverwriteRazorComponentParameterValueTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        return test;
    }

    [Fact]
    public Task AssignParameterInMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                void A()
                {
                    [|Param1 = ""|];
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; } = "Value";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Ctor()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                public Test() => Param1 = "Value";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Dispose()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase, IDisposable
            {
                [Parameter]
                public string Param1 { get; set; }

                public void Dispose() => Param1 = "Value";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task DisposeAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase, IAsyncDisposable
            {
                [Parameter]
                public string Param1 { get; set; }

                public async ValueTask DisposeAsync() => Param1 = "Value";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitializedAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                protected override void OnInitialized()
                {
                    Param1 = "Value";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OnInitializedAsyncAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                protected override async Task OnInitializedAsync()
                {
                    Param1 = "Value";
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SetParametersAsync()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;
            class Test : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                public override async Task SetParametersAsync(ParameterView parameters)
                {
                    Param1 = "Value";
                }
            }
            """;

        return test.RunAsync();
    }
}
