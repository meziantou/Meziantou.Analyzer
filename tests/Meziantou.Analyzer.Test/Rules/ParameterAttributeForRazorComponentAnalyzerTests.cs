using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class ParameterAttributeForRazorComponentAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ParameterAttributeForRazorComponentAnalyzer>()
            .WithTargetFramework(TargetFramework.AspNetCore6_0);
    }

    [Fact]
    public async Task SupplyParameterFromQuery_MissingParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [SupplyParameterFromQuery]
    public int [||]A { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_WithParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [Parameter]
    [SupplyParameterFromQuery]
    public int A { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SupplyParameterFromQuery_WithCascadingParameter()
    {
        const string SourceCode = @"
using Microsoft.AspNetCore.Components;

class Test
{
    [CascadingParameter]
    [SupplyParameterFromQuery]
    public int [||]A { get; set; }
}";
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
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
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
    public int A { get; set; }

    public int B { get; set; }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
