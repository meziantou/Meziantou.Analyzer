using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class SequenceNumberMustBeAConstantAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<SequenceNumberMustBeAConstantAnalyzer>()
            .WithTargetFramework(TargetFramework.AspNetCore7_0);
    }

    [Theory]
    [InlineData("builder.AddAttribute(0, frame: default)")]
    [InlineData("builder.AddAttribute(0, name: default(string))")]
    [InlineData("builder.AddAttribute(0, name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback))")]
    [InlineData("builder.AddAttribute(0, name: default(string), value: false)")]
    [InlineData("builder.AddAttribute(0, name: default(string), value: default(MulticastDelegate))")]
    [InlineData("builder.AddAttribute(0, name: default(string), value: default(object))")]
    [InlineData("builder.AddAttribute(0, name: default(string), value: default(string))")]
    [InlineData("builder.AddAttribute<int>(0, name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback<int>))")]
    [InlineData("builder.AddComponentReferenceCapture(0, componentReferenceCaptureAction: null)")]
    [InlineData("builder.AddContent(0, markupContent: default(Microsoft.AspNetCore.Components.MarkupString))")]
    [InlineData("builder.AddContent(0, markupContent: default(Microsoft.AspNetCore.Components.MarkupString?))")]
    [InlineData("builder.AddContent(0, fragment: default(Microsoft.AspNetCore.Components.RenderFragment))")]
    [InlineData("builder.AddContent<int>(0, fragment: default(Microsoft.AspNetCore.Components.RenderFragment<int>), value: default(int))")]
    [InlineData("builder.AddContent(0, textContent: default(object))")]
    [InlineData("builder.AddContent(0, textContent: default(string))")]
    [InlineData("builder.AddElementReferenceCapture(0, elementReferenceCaptureAction: default(Action<Microsoft.AspNetCore.Components.ElementReference>))")]
    [InlineData("builder.AddMarkupContent(0, markupContent: default(string))")]
    [InlineData("builder.AddMultipleAttributes(0, default(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>))")]
    [InlineData("builder.OpenComponent(0, componentType: default(Type))")]
    [InlineData("builder.OpenComponent<Microsoft.AspNetCore.Components.IComponent>(0)")]
    [InlineData("builder.OpenElement(0, elementName: default(string))")]
    [InlineData("builder.OpenRegion(0)")]
    [InlineData("builder.CloseRegion()")]
    [InlineData("builder.AddEventStopPropagationAttribute(0, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventPreventDefaultAttribute(0, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventPreventDefaultAttribute(param, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventPreventDefaultAttribute((int)(long)param, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventPreventDefaultAttribute((int)longparam, eventName: default(string), value: false)")]
    public async Task Valid(string code)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode($$"""
using System;    
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
class Test
{
    void BuildRenderTree(RenderTreeBuilder builder, int param, long longparam)
	{
        {{code}};
	}
}
""");

        await project.ValidateAsync();
    }

    [Theory]
    [InlineData("builder.AddAttribute([|value++|], frame: default)")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string))")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback))")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string), value: false)")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string), value: default(MulticastDelegate))")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string), value: default(object))")]
    [InlineData("builder.AddAttribute([|value++|], name: default(string), value: default(string))")]
    [InlineData("builder.AddAttribute<int>([|value++|], name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback<int>))")]
    [InlineData("builder.AddComponentReferenceCapture([|value++|], componentReferenceCaptureAction: null)")]
    [InlineData("builder.AddContent([|value++|], markupContent: default(Microsoft.AspNetCore.Components.MarkupString))")]
    [InlineData("builder.AddContent([|value++|], markupContent: default(Microsoft.AspNetCore.Components.MarkupString?))")]
    [InlineData("builder.AddContent([|value++|], fragment: default(Microsoft.AspNetCore.Components.RenderFragment))")]
    [InlineData("builder.AddContent<int>([|value++|], fragment: default(Microsoft.AspNetCore.Components.RenderFragment<int>), value: 0)")]
    [InlineData("builder.AddContent([|value++|], textContent: default(object))")]
    [InlineData("builder.AddContent([|value++|], textContent: default(string))")]
    [InlineData("builder.AddElementReferenceCapture([|value++|], elementReferenceCaptureAction: default(Action<Microsoft.AspNetCore.Components.ElementReference>))")]
    [InlineData("builder.AddMarkupContent([|value++|], markupContent: default(string))")]
    [InlineData("builder.AddMultipleAttributes([|value++|], attributes: default(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>))")]
    [InlineData("builder.OpenComponent([|value++|], componentType: default(Type))")]
    [InlineData("builder.OpenComponent<Microsoft.AspNetCore.Components.IComponent>([|value++|])")]
    [InlineData("builder.OpenElement([|value++|], elementName: default(string))")]
    [InlineData("builder.OpenRegion([|value++|])")]
    [InlineData("builder.AddEventPreventDefaultAttribute([|value++|], eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute([|value++|], eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute([|param++|], eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute([|(int)longparam++|], eventName: default(string), value: false)")]
    public async Task Variable(string code)
    {
        var project = CreateProjectBuilder()
              .WithSourceCode($$"""
using System;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
class Test
{
    void BuildRenderTree(RenderTreeBuilder builder, int param, long longparam)
	{
        int value = 0;
        {{code}};
	}
}
""");

        await project.ValidateAsync();
    }
}
