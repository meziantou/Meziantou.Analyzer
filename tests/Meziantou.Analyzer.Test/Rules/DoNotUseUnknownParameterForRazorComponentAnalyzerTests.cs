using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseUnknownParameterForRazorComponentAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseUnknownParameterForRazorComponentAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddAspNetCore("8.0.0");
        return test;
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    public Task ValidParameterName(string parameterName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    __builder.AddAttribute(1, "{{parameterName}}", "test");
                    __builder.CloseComponent();
                }
            }

            public class SampleComponent : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                [Parameter]
                public string Param2 { get; set; }

                public string NotAParam3 { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("UnknownParam")]
    [InlineData("NotAParam3")]
    public Task WrongParameterName(string parameterName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    {|MA0115:__builder.AddAttribute(1, "{{parameterName}}", "test")|};
                    __builder.CloseComponent();
                }
            }

            public class SampleComponent : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                [Parameter]
                public string Param2 { get; set; }

                public string NotAParam3 { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    [InlineData("unknownParams")]
    public Task ComponentWithCaptureUnmatchedValues_AnyLowercaseParameterIsValid(string parameterName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    __builder.AddAttribute(1, "{{parameterName}}", "test");
                    __builder.CloseComponent();
                }
            }

            public class SampleComponent : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public string Param2 { get; set; }

                public string NotAParam3 { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("UnknownParams")]
    public Task ComponentWithCaptureUnmatchedValues_PascalCaseParameterIsInvalid(string parameterName)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0115.ReportPascalCaseUnmatchedParameter", "true");
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    {|#0:__builder.AddAttribute(1, "{{parameterName}}", "test")|};
                    __builder.CloseComponent();
                }
            }

            public class SampleComponent : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public string Param2 { get; set; }

                public string NotAParam3 { get; set; }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0115", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("The parameter 'UnknownParams' does not exist on component 'SampleComponent'. Available parameters: Param1, Param2."));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    [InlineData("Param3")]
    [InlineData("UnknownParams")]
    public Task ComponentWithCaptureUnmatchedValues_PascalCaseParameterIsValid(string parameterName)
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0115.ReportPascalCaseUnmatchedParameter", "false");
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    __builder.AddAttribute(1, "{{parameterName}}", "test");
                    __builder.CloseComponent();
                }
            }

            public class SampleComponent : ComponentBase
            {
                [Parameter]
                public string Param1 { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public string Param2 { get; set; }

                public string NotAParam3 { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("Param1")]
    [InlineData("Param2")]
    public Task ValidParameterName_BaseType(string parameterName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    __builder.AddAttribute(1, "{{parameterName}}", "test");
                    __builder.CloseComponent();
                }
            }

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
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("UnknownParam")]
    [InlineData("NotAParam3")]
    public Task WrongParameterName_BaseType(string parameterName)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<SampleComponent>(0);
                    {|MA0115:__builder.AddAttribute(1, "{{parameterName}}", "test")|};
                    __builder.CloseComponent();
                }
            }

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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterInChildContent()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
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
                    {|MA0115:__builder.AddAttribute(6, "Title", "How is Blazor working for you?")|};
                    {|MA0115:__builder.AddAttribute(7, "OtherAttribute", "Test")|};
                    __builder.AddAttribute(8, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                        __builder2.OpenComponent<SampleComponent>(9);
                        __builder2.AddAttribute(10, "Param1", "How is Blazor working for you?");
                        {|MA0115:__builder2.AddAttribute(11, "NestedAttribute", "Dummy")|};
                        __builder2.CloseComponent();
                    }
                    ));
                    __builder.CloseComponent();
                }
            }

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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterInAddComponentParameter_Net8()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.AspNetCore.Components;
            class TypeName : ComponentBase
            {
                protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                {
                    __builder.OpenComponent<CustomComponentBase>(1);
                    {|MA0115:__builder.AddComponentParameter(2, "Text", "DummyDisplayText")|};
                    {|MA0115:__builder.AddComponentParameter(3, "OtherAttribute", "Test")|};
                    __builder.CloseComponent();
                }
            }

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
            """;

        return test.RunAsync();
    }
}
