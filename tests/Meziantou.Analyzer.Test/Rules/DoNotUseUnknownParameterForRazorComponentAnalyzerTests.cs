using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class DoNotUseUnknownParameterForRazorComponentAnalyzerTests
{
    private const string Usings = """"
        using Microsoft.AspNetCore.Components;

        """";

    private const string ComponentWithParameters = """"
        public class SampleComponent : ComponentBase
        {
            [Parameter]
            public string Param1 { get; set; }

            [Parameter]
            public string Param2 { get; set; }

            public string NotAParam3 { get; set; }
        }
        """";

    private const string ComponentWithCaptureUnmatchedValues = """"
        public class SampleComponent : ComponentBase
        {
            [Parameter]
            public string Param1 { get; set; }

            [Parameter(CaptureUnmatchedValues = true)]
            public string Param2 { get; set; }

            public string NotAParam3 { get; set; }
        }
        """";

    private const string ComponentWithInheritedParameter = """"
        public class CustomComponentBase : ComponentBase
        {
            [Parameter]
            public string Param1 { get; set; }

            public string NotAParam3 { get; set; }
        }

        public class SampleComponent : CustomComponentBase
        {
            [Parameter]
            public string Param2 { get; set; }
        }
        """";

    private const string ComponentWithChildContent = """"
        public class CustomComponentBase : ComponentBase
        {
            [Parameter]
            public RenderFragment ChildContent { get; set; }
        }

        public class SampleComponent : CustomComponentBase
        {
            [Parameter]
            public string Param1 { get; set; }
        }
        """";

    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseUnknownParameterForRazorComponentAnalyzer>()
            .WithTargetFramework(TargetFramework.AspNetCore8_0);
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    public async Task ValidParameterName(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        __builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithParameters)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("UnknownParam")]
    [InlineData("NotAParam3")]
    public async Task WrongParameterName(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        [||]__builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithParameters)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    [InlineData("unknownParams")]
    public async Task ComponentWithCaptureUnmatchedValues_AnyLowercaseParameterIsValid(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        __builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithCaptureUnmatchedValues)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("UnknownParams")]
    public async Task ComponentWithCaptureUnmatchedValues_PascalCaseParameterIsInvalid(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        [||]__builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithCaptureUnmatchedValues)
              .AddAnalyzerConfiguration("MA0115.ReportPascalCaseUnmatchedParameter", "true")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    [InlineData("Param3")]
    [InlineData("UnknownParams")]
    public async Task ComponentWithCaptureUnmatchedValues_PascalCaseParameterIsValid(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        __builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithCaptureUnmatchedValues)
              .AddAnalyzerConfiguration("MA0115.ReportPascalCaseUnmatchedParameter", "false")
              .ValidateAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    public async Task ValidParameterName_BaseType(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        __builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithInheritedParameter)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("UnknownParam")]
    [InlineData("NotAParam3")]
    public async Task WrongParameterName_BaseType(string parameterName)
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<SampleComponent>(0);
        [||]__builder.AddAttribute(1, "{{parameterName}}", "test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithInheritedParameter)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterInChildContent()
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<global::Microsoft.AspNetCore.Components.Web.PageTitle>(0);
        __builder.AddAttribute(1, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
            __builder2.AddContent(2, "Dummy");
        }
        ));
        __builder.CloseComponent();
        __builder.AddMarkupContent(3, "\r\n\r\n");
        __builder.OpenComponent<CustomComponentBase>(5);
        [||]__builder.AddAttribute(6, "Title", "How is Blazor working for you?");
        [||]__builder.AddAttribute(7, "OtherAttribute", "Test");
        __builder.AddAttribute(8, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
            __builder2.OpenComponent<SampleComponent>(9);
            __builder2.AddAttribute(10, "Param1", "How is Blazor working for you?");
            [||]__builder2.AddAttribute(11, "NestedAttribute", "Dummy");
            __builder2.CloseComponent();
        }
        ));
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithChildContent)
              .ValidateAsync();
    }

    [Fact]
    public async Task InvalidParameterInAddComponentParameter_Net8()
    {
        var sourceCode = $$"""
class TypeName : ComponentBase
{
    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
    {
        __builder.OpenComponent<CustomComponentBase>(1);
        [||]__builder.AddComponentParameter(2, "Text", "DummyDisplayText");
        [||]__builder.AddComponentParameter(3, "OtherAttribute", "Test");
        __builder.CloseComponent();
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(Usings + sourceCode + ComponentWithChildContent)
              .ValidateAsync();
    }

}
