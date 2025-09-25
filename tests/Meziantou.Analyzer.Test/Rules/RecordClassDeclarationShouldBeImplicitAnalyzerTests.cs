#if CSHARP10_OR_GREATER
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RecordClassDeclarationShouldBeImplicitAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RecordClassDeclarationShouldBeImplicitAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ExplicitRecordClass_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithModifiers_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public sealed record [|class|] Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordStruct_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record struct Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

[Fact]
    public async Task RegularClass_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public class Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task RegularStruct_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public struct Target { public int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithParameters_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target(int Id);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_WithParameters_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record Target(int Id);
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_Generic_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target<T> { public required T Value { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_InNamespace_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                namespace MyNamespace
                {
                    public record [|class|] Target { public required int Id { get; init; } }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithInheritance_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public abstract record BaseRecord;            
                public record [|class|] Target : BaseRecord { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }
}
#endif