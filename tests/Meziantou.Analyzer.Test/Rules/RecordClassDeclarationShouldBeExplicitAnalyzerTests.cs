#if CSHARP10_OR_GREATER
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RecordClassDeclarationShouldBeExplicitAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RecordClassDeclarationShouldBeExplicitAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ImplicitRecordClass_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public [|record|] Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_WithModifiers_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public sealed [|record|] Target { public required int Id { get; init; } }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record class Target { public required int Id { get; init; } }
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
    public async Task ImplicitRecordClass_WithParameters_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public [|record|] Target(int Id) { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithParameters_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record class Target(int Id) { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_Generic_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public [|record|] Target<T> { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_InNamespace_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                namespace MyNamespace
                {
                    public [|record|] Target { }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_WithInheritance_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public abstract [|record|] BaseRecord { }           
                public [|record|] Target : BaseRecord { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task RxplicitRecordClass_WithInheritance_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public abstract record class BaseRecord { }
                public record class Target : BaseRecord { }
                """)
            .ValidateAsync();
    }
}
#endif