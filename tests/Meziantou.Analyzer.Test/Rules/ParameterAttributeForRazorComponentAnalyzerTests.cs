using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class ParameterAttributeForRazorComponentAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ParameterAttributeForRazorComponentAnalyzer>()
            .WithCodeFixProvider<ParameterAttributeForRazorComponentFixer>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task SupplyParameterFromQuery_MissingParameter()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;

[Route("/test")]
class Test
{
    [SupplyParameterFromQuery]
    public int [||]A { get; set; }
}
""";
        const string Fix = """
using Microsoft.AspNetCore.Components;

[Route("/test")]
class Test
{
    [SupplyParameterFromQuery]
    [Parameter]
    public int A { get; set; }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_MissingParameter_AspNetCore8()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;

[Route("/test")]
class Test
{
    [SupplyParameterFromQuery]
    public int A { get; set; }
}
""";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .WithTargetFramework(TargetFramework.AspNetCore8_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_WithParameter()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;

[Route("/test")]
class Test
{
    [Parameter]
    [SupplyParameterFromQuery]
    public int A { get; set; }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_WithCascadingParameter()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;

[Route("/test")]
class Test
{
    [CascadingParameter]
    [SupplyParameterFromQuery]
    public int [||]A { get; set; }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_NonRoutable()
    {
        const string SourceCode = """
using Microsoft.AspNetCore.Components;

class Test
{
    [Parameter, SupplyParameterFromQuery]
    public int [||]A { get; set; }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditorRequired_MissingParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [EditorRequired]
    public int [||]A { get; set; }
}";
        const string Fix = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [EditorRequired]
    [Parameter]
    public int A { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditorRequired_WithParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [Parameter]
    [EditorRequired]
    public int A { get; set; }

    public int B { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task EditorRequired_WithCascadingParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [CascadingParameter]
    [EditorRequired]
    public int [||]A { get; set; }

    public int B { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
