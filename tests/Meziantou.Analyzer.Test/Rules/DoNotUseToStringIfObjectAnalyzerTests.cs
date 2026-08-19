using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class DoNotUseToStringIfObjectAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseToStringIfObjectAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest)
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

#if ROSLYN_5_9_OR_GREATER
    private static ProjectBuilder CreatePreviewProjectBuilder()
    {
        return CreateProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview);
    }
#endif

    [Fact]
    public async Task Object_ToString()
    {
        var sourceCode = """
var o = new object();
o.ToString();
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Struct_ToString()
    {
        var sourceCode = """
var o = new A();
[|o.ToString()|];

public struct A{ }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedRecord_ToString()
    {
        var sourceCode = """
var o = new A();
o.ToString();

public sealed record A();
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedClass_ToString()
    {
        var sourceCode = """
var o = new A();
[|o.ToString()|];

public sealed class A {}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedClass_Overridden_ToString()
    {
        var sourceCode = """
var o = new A();
o.ToString();

public sealed class A { public override string ToString() => throw null;}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Struct_NoToStringOverride()
    {
        var sourceCode = """
Sample a = new Sample();
[|a.ToString()|];

struct Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Struct_ToStringOverride()
    {
        var sourceCode = """
Sample a = new Sample();
a.ToString();

struct Sample { public override string ToString() => throw null; }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonSealedBaseType_RuntimeTypeOverridesToString()
    {
        var sourceCode = """
Sample a = new Derived();
a.ToString();

class Sample { }
class Derived : Sample { public override string ToString() => "test"; }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_RuntimeTypeOverridesToString()
    {
        var sourceCode = """
var a = new Derived();
[|a.ToString()|];

class Sample { }
sealed class Derived : Sample {  }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromLocalInitializer()
    {
        var sourceCode = """
Sample a = new Derived();
[|a.ToString()|];

class Sample { }
sealed class Derived : Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromLocalAssignment()
    {
        var sourceCode = """
Sample a;
a = new Derived();
[|a.ToString()|];

class Sample { }
sealed class Derived : Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromLocalInitializer_Reassigned()
    {
        var sourceCode = """
Sample a = new Derived();
a = Get();
a.ToString();

Sample Get() => new Sample();

class Sample { }
sealed class Derived : Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromPrivateReadonlyFieldInitializer()
    {
        var sourceCode = """
class Test
{
    private readonly Sample _value = new Derived();

    void M()
    {
        [|_value.ToString()|];
    }
}

class Sample { }
sealed class Derived : Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_PrivateReadonlyFieldInitializer_AssignedInConstructor()
    {
        var sourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromPrivateGetOnlyPropertyInitializer()
    {
        var sourceCode = """
class Test
{
    private Sample Value { get; } = new Derived();

    void M()
    {
        [|Value.ToString()|];
    }
}

class Sample { }
sealed class Derived : Sample { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_PrivateGetOnlyPropertyInitializer_AssignedInConstructor()
    {
        var sourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_FlowedFromLocalInitializer_InheritsBaseToStringOverride()
    {
        var sourceCode = """
object o = "abc";
o.ToString();
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedType_InheritsBaseToStringOverride()
    {
        var sourceCode = """
var a = new Derived();
a.ToString();

class Base { public override string ToString() => "test"; }
sealed class Derived : Base { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if ROSLYN_5_9_OR_GREATER
    [Fact]
    public async Task ClosedType_NoToStringOverrideInHierarchy()
    {
        var sourceCode = """
Shape a = new Circle();
[|((Shape)a).ToString()|];

closed class Shape;
sealed class Circle : Shape;
sealed class Square : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_DerivedTypeOverridesToString()
    {
        var sourceCode = """
Shape a = new Circle();
((Shape)a).ToString();

closed class Shape;
sealed class Circle : Shape;
sealed class Square : Shape { public override string ToString() => "square"; }
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_NestedClosedHierarchy_NoToStringOverride()
    {
        var sourceCode = """
Shape a = new Circle();
[|((Shape)a).ToString()|];

closed class Shape;
closed class Round : Shape;
sealed class Circle : Round;
sealed class Square : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_NestedClosedHierarchy_LeafOverridesToString()
    {
        var sourceCode = """
Shape a = new Circle();
((Shape)a).ToString();

closed class Shape;
closed class Round : Shape;
sealed class Circle : Round { public override string ToString() => "circle"; }
sealed class Square : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_NestedClosedHierarchy_IntermediateTypeOverridesToString()
    {
        var sourceCode = """
Shape a = new Circle();
((Shape)a).ToString();

closed class Shape;
closed class Round : Shape { public override string ToString() => "round"; }
sealed class Circle : Round;
sealed class Square : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_DerivedTypeIsNeitherSealedNorClosed()
    {
        var sourceCode = """
Shape a = new Circle();
((Shape)a).ToString();

closed class Shape;
class Circle : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_OverridesToString()
    {
        var sourceCode = """
Shape a = new Circle();
((Shape)a).ToString();

closed class Shape { public override string ToString() => "shape"; }
sealed class Circle : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ClosedType_UnspeakableDerivedType()
    {
        var sourceCode = """
class Test
{
    void M<T>(Shape<T> value) => value.ToString();
}

closed class Shape<T>;
sealed class Circle : Shape<int>;
sealed class Square<T> : Shape<T[]>;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_ClosedType_NoToStringOverrideInHierarchy()
    {
        var sourceCode = """
Shape a = new Circle();
_ = $"{[|(Shape)a|]}";

closed class Shape;
sealed class Circle : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_ClosedType_DerivedTypeOverridesToString()
    {
        var sourceCode = """
Shape a = new Circle();
_ = $"{(Shape)a}";

closed class Shape;
sealed class Circle : Shape { public override string ToString() => "circle"; }
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Concat_ClosedType_NoToStringOverrideInHierarchy()
    {
        var sourceCode = """
Shape a = new Circle();
_ = "" + [|(Shape)a|];

closed class Shape;
sealed class Circle : Shape;
""";
        await CreatePreviewProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task InterpolatedString_Sealed_Interpolation()
    {
        var sourceCode = """
var o = new A();
_ = $"{[|o|]}";

public sealed class A { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_SealedType_InheritsBaseToStringOverride()
    {
        var sourceCode = """
var a = new Derived();
_ = $"{a}";

class Base { public override string ToString() => "test"; }
sealed class Derived : Base { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Interpolation()
    {
        var sourceCode = """
var o = new A();
_ = $"{o}";

public class A { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Struct_Interpolation()
    {
        var sourceCode = """
var o = new A();
_ = $"{[|o|]}";

public struct A { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Struct_Overridden_Interpolation()
    {
        var sourceCode = """
var o = new A();
_ = $"{o}";

public struct A { public override string ToString() => throw null; }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Enum_Interpolation()
    {
        var sourceCode = """
var o = System.DayOfWeek.Monday;
_ = $"{o}";
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Struct_Interpolation_Net8()
    {
        var sourceCode = """
var o = new A();
System.Diagnostics.Debug.Assert(false, $"foo{[|o|]}bar");

public struct A { }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.Net8_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_Struct_Interpolation_CustomStringHandler()
    {
        var sourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.Net8_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task InterpolatedString_SealedType_CustomStringHandler()
    {
        var sourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.Net8_0)
              .ValidateAsync();
    }

    [Fact]
    public async Task Object_Concat()
    {
        var sourceCode = """
var o = new object();
_ = "" + o;
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Struct_Concat()
    {
        var sourceCode = """
var o = new A();
_ = "" + [|o|];

public struct A{ }
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedRecord_Concat()
    {
        var sourceCode = """
var o = new A();
_ = "" + o;

public sealed record A();
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task SealedClass_Concat()
    {
        var sourceCode = """
var o = new A();
_ = "" + [|o.ToString()|];

public sealed class A {}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interpolation_Int32()
    {
        var sourceCode = """
var statusCode = 42;
_ = $"{statusCode}";
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interpolation_CastEnumToInt32()
    {
        var sourceCode = """
var statusCode = System.Net.HttpStatusCode.OK;
_ = $"{(int)statusCode}";
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interpolation_Enum()
    {
        var sourceCode = """
var statusCode = System.Net.HttpStatusCode.OK;
_ = $"{statusCode}";
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interpolation_AnonymousType()
    {
        var sourceCode = """
var obj = new { FirstName = "" };
_ = $"{obj}";
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ToString_AnonymousType()
    {
        var sourceCode = """
new { FirstName = "" }.ToString();
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interpolation_ReproCachingIssue()
    {
        var sourceCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
}
