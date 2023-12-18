﻿using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

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

#if CSHARP10_OR_GREATER
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
#endif

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
    public async Task SealedClass_Overridden_Concat()
    {
        var sourceCode = """
var o = new A();
_ = "" + o;

public sealed class A { public override string ToString() => throw null;}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

}
