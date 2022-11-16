using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class DoNotOverwriteRazorComponentParameterValueTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotOverwriteRazorComponentParameterValue>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task AssignParameterInMethod()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;
class Test : ComponentBase
{
    [Parameter]
    public string Param1 { get; set; }

    void A()
    {
        [||]Param1 = "";            
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task PropertyInitializer()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;
class Test : ComponentBase
{
    [Parameter]
    public string Param1 { get; set; } = "Value";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Ctor()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;
class Test : ComponentBase
{
    [Parameter]
    public string Param1 { get; set; }

    public Test() => Param1 = "Value";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Dispose()
    {
        const string SourceCode = """
using System;
using Microsoft.AspNetCore.Components;
class Test : ComponentBase, IDisposable
{
    [Parameter]
    public string Param1 { get; set; }

    public void Dispose() => Param1 = "Value";
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task DisposeAsync()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitializedAsync()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OnInitializedAsyncAsync()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SetParametersAsync()
    {
        const string SourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
