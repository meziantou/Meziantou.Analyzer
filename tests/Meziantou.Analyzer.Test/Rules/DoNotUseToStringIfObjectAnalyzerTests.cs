using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotUseToStringIfObjectAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseToStringIfObjectAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

#if ROSLYN_5_9_OR_GREATER
    private static AnalyzerTest CreatePreviewTest()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.Preview;
        return test;
    }
#endif

    [Fact]
    public Task Object_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new object();
            o.ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            {|MA0150:o.ToString()|};

            public struct A{ }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedRecord_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            o.ToString();

            public sealed record A();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedClass_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            {|MA0150:o.ToString()|};

            public sealed class A {}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedClass_Overridden_ToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            o.ToString();

            public sealed class A { public override string ToString() => throw null;}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_NoToStringOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = new Sample();
            {|MA0150:a.ToString()|};

            struct Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_ToStringOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = new Sample();
            a.ToString();

            struct Sample { public override string ToString() => throw null; }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonSealedBaseType_RuntimeTypeOverridesToString()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = new Derived();
            a.ToString();

            class Sample { }
            class Derived : Sample { public override string ToString() => "test"; }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_RuntimeTypeOverridesToString()
    {
        var test = CreateTest();
        test.TestCode = """
            var a = new Derived();
            {|MA0150:a.ToString()|};

            class Sample { }
            sealed class Derived : Sample {  }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromLocalInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = new Derived();
            {|MA0150:a.ToString()|};

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromLocalAssignment()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a;
            a = new Derived();
            {|MA0150:a.ToString()|};

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromLocalInitializer_Reassigned()
    {
        var test = CreateTest();
        test.TestCode = """
            Sample a = new Derived();
            a = Get();
            a.ToString();

            Sample Get() => new Sample();

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromPrivateReadonlyFieldInitializer()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            class Test
            {
                private readonly Sample _value = new Derived();

                void M()
                {
                    {|MA0150:_value.ToString()|};
                }
            }

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_PrivateReadonlyFieldInitializer_AssignedInConstructor()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            class Test
            {
                private readonly Sample _value = new Derived();

                public Test()
                {
                    _value = new Sample();
                }

                void M()
                {
                    _value.ToString();
                }
            }

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromPrivateGetOnlyPropertyInitializer()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            class Test
            {
                private Sample Value { get; } = new Derived();

                void M()
                {
                    {|MA0150:Value.ToString()|};
                }
            }

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_PrivateGetOnlyPropertyInitializer_AssignedInConstructor()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            class Test
            {
                private Sample Value { get; } = new Derived();

                public Test()
                {
                    Value = new Sample();
                }

                void M()
                {
                    Value.ToString();
                }
            }

            class Sample { }
            sealed class Derived : Sample { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_FlowedFromLocalInitializer_InheritsBaseToStringOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            object o = "abc";
            o.ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedType_InheritsBaseToStringOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            var a = new Derived();
            a.ToString();

            class Base { public override string ToString() => "test"; }
            sealed class Derived : Base { }
            """;

        return test.RunAsync();
    }

#if ROSLYN_5_9_OR_GREATER
    [Fact]
    public Task ClosedType_NoToStringOverrideInHierarchy()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            {|MA0150:((Shape)a).ToString()|};

            closed class Shape;
            sealed class Circle : Shape;
            sealed class Square : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_DerivedTypeOverridesToString()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            ((Shape)a).ToString();

            closed class Shape;
            sealed class Circle : Shape;
            sealed class Square : Shape { public override string ToString() => "square"; }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_NestedClosedHierarchy_NoToStringOverride()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            {|MA0150:((Shape)a).ToString()|};

            closed class Shape;
            closed class Round : Shape;
            sealed class Circle : Round;
            sealed class Square : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_NestedClosedHierarchy_LeafOverridesToString()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            ((Shape)a).ToString();

            closed class Shape;
            closed class Round : Shape;
            sealed class Circle : Round { public override string ToString() => "circle"; }
            sealed class Square : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_NestedClosedHierarchy_IntermediateTypeOverridesToString()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            ((Shape)a).ToString();

            closed class Shape;
            closed class Round : Shape { public override string ToString() => "round"; }
            sealed class Circle : Round;
            sealed class Square : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_DerivedTypeIsNeitherSealedNorClosed()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            ((Shape)a).ToString();

            closed class Shape;
            class Circle : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_OverridesToString()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            ((Shape)a).ToString();

            closed class Shape { public override string ToString() => "shape"; }
            sealed class Circle : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ClosedType_UnspeakableDerivedType()
    {
        var test = CreatePreviewTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            class Test
            {
                void M<T>(Shape<T> value) => value.ToString();
            }

            closed class Shape<T>;
            sealed class Circle : Shape<int>;
            sealed class Square<T> : Shape<T[]>;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_ClosedType_NoToStringOverrideInHierarchy()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            _ = $"{{|MA0150:(Shape)a|}}";

            closed class Shape;
            sealed class Circle : Shape;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_ClosedType_DerivedTypeOverridesToString()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            _ = $"{(Shape)a}";

            closed class Shape;
            sealed class Circle : Shape { public override string ToString() => "circle"; }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Concat_ClosedType_NoToStringOverrideInHierarchy()
    {
        var test = CreatePreviewTest();
        test.TestCode = """
            Shape a = new Circle();
            _ = "" + {|MA0150:(Shape)a|};

            closed class Shape;
            sealed class Circle : Shape;
            """;

        return test.RunAsync();
    }
#endif

    [Fact]
    public Task InterpolatedString_Sealed_Interpolation()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = $"{{|MA0150:o|}}";

            public sealed class A { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_SealedType_InheritsBaseToStringOverride()
    {
        var test = CreateTest();
        test.TestCode = """
            var a = new Derived();
            _ = $"{a}";

            class Base { public override string ToString() => "test"; }
            sealed class Derived : Base { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Interpolation()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = $"{o}";

            public class A { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Struct_Interpolation()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = $"{{|MA0150:o|}}";

            public struct A { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Struct_Overridden_Interpolation()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = $"{o}";

            public struct A { public override string ToString() => throw null; }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Enum_Interpolation()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = System.DayOfWeek.Monday;
            _ = $"{o}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Struct_Interpolation_Net8()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            var o = new A();
            System.Diagnostics.Debug.Assert(false, $"foo{{|MA0150:o|}}bar");

            public struct A { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_Struct_Interpolation_CustomStringHandler()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            var o = new A();
            Foo($"foo{o}bar");

            void Foo(CustomStringHandler handler) => throw null;

            public struct A { }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public struct CustomStringHandler
            {
                public CustomStringHandler(int literalLength, int formattedCount) => throw null;
                public CustomStringHandler(int literalLength, int formattedCount, System.IFormatProvider? provider) => throw null;
                public void AppendLiteral(string value) => throw null;
                public void AppendFormatted<T>(T value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InterpolatedString_SealedType_CustomStringHandler()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            var o = new A();
            Foo($"foo{o}bar");

            void Foo(CustomStringHandler handler) => throw null;

            public sealed class A { }

            [System.Runtime.CompilerServices.InterpolatedStringHandler]
            public struct CustomStringHandler
            {
                public CustomStringHandler(int literalLength, int formattedCount) => throw null;
                public void AppendLiteral(string value) => throw null;
                public void AppendFormatted<T>(T value) => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Object_Concat()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new object();
            _ = "" + o;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_Concat()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = "" + {|MA0150:o|};

            public struct A{ }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedRecord_Concat()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = "" + o;

            public sealed record A();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SealedClass_Concat()
    {
        var test = CreateTest();
        test.TestCode = """
            var o = new A();
            _ = "" + {|MA0150:o.ToString()|};

            public sealed class A {}
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interpolation_Int32()
    {
        var test = CreateTest();
        test.TestCode = """
            var statusCode = 42;
            _ = $"{statusCode}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interpolation_CastEnumToInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            var statusCode = System.Net.HttpStatusCode.OK;
            _ = $"{(int)statusCode}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interpolation_Enum()
    {
        var test = CreateTest();
        test.TestCode = """
            var statusCode = System.Net.HttpStatusCode.OK;
            _ = $"{statusCode}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interpolation_AnonymousType()
    {
        var test = CreateTest();
        test.TestCode = """
            var obj = new { FirstName = "" };
            _ = $"{obj}";
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ToString_AnonymousType()
    {
        var test = CreateTest();
        test.TestCode = """
            new { FirstName = "" }.ToString();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interpolation_ReproCachingIssue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            var url = args[0];
            var result = new Result(default);
            var encoding = "";
            Assert.False(result.IsSuccessStatusCode, $"{url}\nEncoding: {encoding}\nStatus code: {(int)result.StatusCode} {result.StatusCode}");
            Assert.True(result.IsSuccessStatusCode, $"{url}\nEncoding: {encoding}\nStatus code: {(int)result.StatusCode} {result.StatusCode}");

            class Assert
            {
                public static void False(bool condition, string? errorMessage) { }
                public static void True(bool condition, string? errorMessage) { }
            }

            record Result(System.Net.HttpStatusCode StatusCode)
            {
                public bool IsSuccessStatusCode => false;
            }
            """;

        return test.RunAsync();
    }
}
