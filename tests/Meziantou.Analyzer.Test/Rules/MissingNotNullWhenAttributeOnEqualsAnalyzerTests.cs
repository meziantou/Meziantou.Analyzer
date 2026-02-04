using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<MissingNotNullWhenAttributeOnEqualsAnalyzer>();
    }

    [Fact]
    public async Task Equals_Object_WithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public override bool Equals(object? [|obj|])
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_Object_WithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Diagnostics.CodeAnalysis;

                class Sample
                {
                    public override bool Equals([NotNullWhen(true)] object? obj)
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_WithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Sample : IEquatable<Sample>
                {
                    public bool Equals(Sample? [|other|])
                    {
                        return false;
                    }

                    public override bool Equals(object? [|obj|])
                    {
                        return Equals(obj as Sample);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_WithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;
                using System.Diagnostics.CodeAnalysis;

                class Sample : IEquatable<Sample>
                {
                    public bool Equals([NotNullWhen(true)] Sample? other)
                    {
                        return false;
                    }

                    public override bool Equals([NotNullWhen(true)] object? obj)
                    {
                        return Equals(obj as Sample);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_NonNullableParameter_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public override bool Equals(object obj)
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEqualsMethod_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public bool IsEqual(object? obj)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PrivateEquals_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    private bool Equals(object? obj)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StaticEquals_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public static bool Equals(object? obj1, object? obj2)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_WrongSignature_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public bool Equals(object? obj, int x)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_BothMethodsWithoutAttribute_ShouldReportBothDiagnostics()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Sample : IEquatable<Sample>
                {
                    public bool Equals(Sample? [|other|])
                    {
                        return false;
                    }

                    public override bool Equals(object? [|obj|])
                    {
                        return Equals(obj as Sample);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }
}
