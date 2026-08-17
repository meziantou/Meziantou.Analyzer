#if ROSLYN_5_9_OR_GREATER
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryClosedModifierAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithLanguageVersion(LanguageVersion.Preview)
            .WithTargetFramework(TargetFramework.Net11_0)
            .WithAnalyzer<RemoveUnnecessaryClosedModifierAnalyzer>()
            .WithCodeFixProvider<RemoveUnnecessaryClosedModifierFixer>();
    }

    [Fact]
    public async Task ClosedClass_WithoutDerivedType_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|closed|] class Sample
            {
            }
            """;

        const string CodeFix = """
            class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedClass_WithDerivedType_NoDiagnostic()
    {
        const string SourceCode = """
            closed class Sample
            {
            }

            class Derived : Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedClass_WithDerivedTypeInAnotherFile_NoDiagnostic()
    {
        var builder = CreateProjectBuilder();
        builder.ApiReferences.Add("class Derived : Sample;");

        await builder
            .WithSourceCode("closed class Sample;")
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedRecord_WithoutDerivedType_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|closed|] record Sample;
            """;

        const string CodeFix = """
            record Sample;
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedRecord_WithDerivedType_NoDiagnostic()
    {
        const string SourceCode = """
            closed record Sample;
            record Derived : Sample;
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task NonClosedClass_NoDiagnostic()
    {
        const string SourceCode = """
            class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedClass_WithOtherModifiers_ReportsDiagnostic()
    {
        const string SourceCode = """
            public [|closed|] partial class Sample
            {
            }
            """;

        const string CodeFix = """
            public partial class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedClass_PreserveComments_ReportsDiagnostic()
    {
        const string SourceCode = """
            /*sample*/[|closed|] class Sample
            {
            }
            """;

        const string CodeFix = """
            /*sample*/class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedPartialClass_WithoutDerivedType_ReportsDiagnosticOnDeclarationWithModifier()
    {
        const string SourceCode = """
            [|closed|] partial class Sample
            {
            }

            partial class Sample
            {
            }
            """;

        const string CodeFix = """
            partial class Sample
            {
            }

            partial class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedNestedClass_WithoutDerivedType_ReportsDiagnostic()
    {
        const string SourceCode = """
            class Sample
            {
                [|closed|] class Nested
                {
                }
            }
            """;

        const string CodeFix = """
            class Sample
            {
                class Nested
                {
                }
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedGenericClass_WithDerivedType_NoDiagnostic()
    {
        const string SourceCode = """
            closed class Sample<T>;
            class Derived : Sample<int>;
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedDerivedType_WithoutDerivedType_ReportsDiagnostic()
    {
        const string SourceCode = """
            closed class Sample;
            [|closed|] class Derived : Sample;
            """;

        const string CodeFix = """
            closed class Sample;
            class Derived : Sample;
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }
}
#endif
