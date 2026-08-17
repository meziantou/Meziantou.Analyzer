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
            .WithAnalyzer<RemoveUnnecessaryClosedModifierAnalyzer>();
    }

    [Fact]
    public async Task ClosedClass_WithoutDerivedType_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|closed|] class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
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

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
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

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
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

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
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

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
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

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task ClosedClass_WithAbstractMember_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|closed|] class Sample
            {
                public abstract int Value { get; }
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }
}
#endif
