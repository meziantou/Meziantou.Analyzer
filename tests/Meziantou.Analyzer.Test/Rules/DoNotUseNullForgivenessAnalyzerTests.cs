using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseNullForgivenessAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(Helpers.TargetFramework.Net9_0)
            .WithAnalyzer<DoNotUseNullForgivenessAnalyzer>();
    }

    [Fact]
    public async Task NullForgiveness_NullLiteral_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    string _field = [|null!|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NullForgiveness_DefaultLiteral_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    string _field = [|default!|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NullForgiveness_DefaultExpression_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    string _field = [|default(string)!|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NullForgiveness_Property_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    string Prop { get; set; } = [|null!|];
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NullForgiveness_VariableAssignment_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    void M()
                    {
                        string s = [|null!|];
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NullForgiveness_MemberAccess_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Model
                {
                    public string? Value { get; set; }
                }
                class Sample
                {
                    void M(Model model)
                    {
                        _ = model.Value!.Length;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NoNullForgiveness_NoDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    string _field = "value";
                    string Prop { get; set; } = "value";
                }
                """)
            .ValidateAsync();
    }
}
