using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzer>()
            .WithCodeFixProvider<SimplifyStringCreateWhenAllParametersAreCultureInvariantFixer>()
            .WithTargetFramework(TargetFramework.Net6_0);
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task StringCreateWithInvariantCulture_OnlyCultureInvariantParameters_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:O}.")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"Current time is {DateTime.Now:O}.";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithString_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var name = "test";
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Name: {name}")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var name = "test";
        var x = $"Name: {name}";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithGuid_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var id = Guid.NewGuid();
        var x = [|string.Create(CultureInfo.InvariantCulture, $"ID: {id}")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var id = Guid.NewGuid();
        var x = $"ID: {id}";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithTimeSpanInvariantFormat_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var duration = TimeSpan.FromSeconds(42);
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Duration: {duration:c}")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var duration = TimeSpan.FromSeconds(42);
        var x = $"Duration: {duration:c}";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithCultureSensitiveParameter_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var price = 42.5;
        var x = string.Create(CultureInfo.InvariantCulture, $"Price: {price}");
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithDateTimeNonInvariantFormat_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.InvariantCulture, $"Current time is {DateTime.Now:d}.");
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithCurrentCulture_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = string.Create(CultureInfo.CurrentCulture, $"Current time is {DateTime.Now:O}.");
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_LiteralOnly_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"Hello World")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"Hello World";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_WithInteger_NoDiagnostic()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var count = 42;
        var x = string.Create(CultureInfo.InvariantCulture, $"Count: {count}");
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_EmptyString_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StringCreateWithInvariantCulture_MultipleWords_ShouldReport()
    {
        const string SourceCode = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = [|string.Create(CultureInfo.InvariantCulture, $"This is a test message without any interpolations")|];
    }
}
""";

        const string Fix = """
using System;
using System.Globalization;

class TypeName
{
    public void Test()
    {
        var x = $"This is a test message without any interpolations";
    }
}
""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithTargetFramework(TargetFramework.Net6_0)
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(Fix)
              .ValidateAsync();
    }
#endif
}
