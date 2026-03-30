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
            .WithCodeFixProvider<RecordClassDeclarationShouldBeImplicitFixer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ExplicitRecordClass_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithModifiers_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public sealed record [|class|] Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordStruct_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record struct Target { }
                """)
            .ValidateAsync();
    }

[Fact]
    public async Task RegularClass_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public class Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task RegularStruct_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public struct Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithParameters_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target(int Id) { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ImplicitRecordClass_WithParameters_ShouldNotReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record Target(int Id) { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_Generic_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target<T> { }
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
                    public record [|class|] Target { }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task ExplicitRecordClass_WithInheritance_ShouldReportDiagnostic()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                public abstract record BaseRecord { }
                public record [|class|] Target : BaseRecord { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Fix_ExplicitRecordClass()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target { }
                """)
            .ShouldFixCodeWith("""
                public record Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Fix_ExplicitRecordClass_WithModifiers()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                public sealed record [|class|] Target { }
                """)
            .ShouldFixCodeWith("""
                public sealed record Target { }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Fix_ExplicitRecordClass_WithParameters()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                public record [|class|] Target(int Id) { }
                """)
            .ShouldFixCodeWith("""
                public record Target(int Id) { }
                """)
            .ValidateAsync();
    }
}
#endif