using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.SequenceNumberMustBeAConstantAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class SequenceNumberMustBeAConstantAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddAspNetCore();
        return test;
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
    public Task Valid(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
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
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, frame: default)")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string))")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback))")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string), value: false)")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string), value: default(MulticastDelegate))")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string), value: default(object))")]
    [InlineData("builder.AddAttribute({|MA0123:value++|}, name: default(string), value: default(string))")]
    [InlineData("builder.AddAttribute<int>({|MA0123:value++|}, name: default(string), value: default(Microsoft.AspNetCore.Components.EventCallback<int>))")]
    [InlineData("builder.AddComponentReferenceCapture({|MA0123:value++|}, componentReferenceCaptureAction: null)")]
    [InlineData("builder.AddContent({|MA0123:value++|}, markupContent: default(Microsoft.AspNetCore.Components.MarkupString))")]
    [InlineData("builder.AddContent({|MA0123:value++|}, markupContent: default(Microsoft.AspNetCore.Components.MarkupString?))")]
    [InlineData("builder.AddContent({|MA0123:value++|}, fragment: default(Microsoft.AspNetCore.Components.RenderFragment))")]
    [InlineData("builder.AddContent<int>({|MA0123:value++|}, fragment: default(Microsoft.AspNetCore.Components.RenderFragment<int>), value: 0)")]
    [InlineData("builder.AddContent({|MA0123:value++|}, textContent: default(object))")]
    [InlineData("builder.AddContent({|MA0123:value++|}, textContent: default(string))")]
    [InlineData("builder.AddElementReferenceCapture({|MA0123:value++|}, elementReferenceCaptureAction: default(Action<Microsoft.AspNetCore.Components.ElementReference>))")]
    [InlineData("builder.AddMarkupContent({|MA0123:value++|}, markupContent: default(string))")]
    [InlineData("builder.AddMultipleAttributes({|MA0123:value++|}, attributes: default(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string,object>>))")]
    [InlineData("builder.OpenComponent({|MA0123:value++|}, componentType: default(Type))")]
    [InlineData("builder.OpenComponent<Microsoft.AspNetCore.Components.IComponent>({|MA0123:value++|})")]
    [InlineData("builder.OpenElement({|MA0123:value++|}, elementName: default(string))")]
    [InlineData("builder.OpenRegion({|MA0123:value++|})")]
    [InlineData("builder.AddEventPreventDefaultAttribute({|MA0123:value++|}, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute({|MA0123:value++|}, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute({|MA0123:param++|}, eventName: default(string), value: false)")]
    [InlineData("builder.AddEventStopPropagationAttribute({|MA0123:(int)longparam++|}, eventName: default(string), value: false)")]
    public Task Variable(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
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
            """;

        return test.RunAsync();
    }
}
