#if CSHARP12_OR_GREATER
using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RedundantBackingFieldForPrimaryConstructorParameterAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RedundantBackingFieldForPrimaryConstructorParameterAnalyzer>()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12);
    }

    private static ProjectBuilder CreateProjectBuilderWithFixer()
    {
        return CreateProjectBuilder()
            .WithCodeFixProvider<RedundantBackingFieldForPrimaryConstructorParameterFixer>();
    }

    [Fact]
    public async Task RedundantField_DirectCopy_IsFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;
                }
                """)
              .ShouldReportDiagnosticWithMessage("Field '_x' is a redundant copy of primary constructor parameter 'x'. Use the parameter directly.")
              .ValidateAsync();
    }

    [Fact]
    public async Task RedundantField_WideningConversion_IsFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly long [|_x|] = x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RedundantField_NullableLifting_IsFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int? [|_x|] = x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RedundantField_MultipleFields_AllFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int a, int b)
                {
                    private readonly int [|_a|] = a, [|_b|] = b;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RedundantField_NonReadonly_IsFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private int [|_x|] = x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task RedundantField_Record_IsFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                record R(int x)
                {
                    private readonly int [|_x|] = x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_TransformedValue_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int _x = x + 1;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_MethodCallOnParameter_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(string s)
                {
                    private readonly string _s = System.IO.Path.GetFullPath(s);
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_ExplicitCast_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(object o)
                {
                    private readonly string _s = (string)o;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_ClassicConstructor_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo
                {
                    private readonly int _x;
                    public Foo(int x) { _x = x; }
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_NoInitializer_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int _x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_LocalReference_NotFlagged()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int _x = GetDefault();
                    private static int GetDefault() => 0;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_UseParameterDirectly_RemovesFieldAndReplacesReferences()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;

                    public int M() => _x;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                class Foo(int x)
                {
                    public int M() => x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_UseParameterDirectly_ReplacesThisQualifiedReference()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;

                    public int M() => this._x;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                class Foo(int x)
                {
                    public int M() => x;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_UseParameterDirectly_NoReferences()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                class Foo(int x)
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_UseParameterDirectly_NonReadonlyField()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private int [|_x|] = x;

                    public void M(int v) => _x = v;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                class Foo(int x)
                {
                    public void M(int v) => x = v;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveField_LeavesReferences()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;
                }
                """)
              .ShouldFixCodeWith(index: 0, """
                class Foo(int x)
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_RemoveField_NoReferences()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int [|_x|] = x;
                }
                """)
              .ShouldFixCodeWith(index: 0, """
                class Foo(int x)
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_WideningConversion_OnlyRemoveFieldOffered()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly long [|_x|] = x;
                }
                """)
              .ShouldFixCodeWith(index: 0, """
                class Foo(int x)
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_NullableLifting_OnlyRemoveFieldOffered()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int x)
                {
                    private readonly int? [|_x|] = x;
                }
                """)
              .ShouldFixCodeWith(index: 0, """
                class Foo(int x)
                {
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_MultipleDeclarators_RemovesOneOnly()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                class Foo(int a, int b)
                {
                    private readonly int [|_a|] = a, [|_b|] = b;

                    public int M() => _a + _b;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                class Foo(int a, int b)
                {
                    private readonly int _b = b;

                    public int M() => a + _b;
                }
                """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_Record_RemovesFieldAndReplacesReferences()
    {
        await CreateProjectBuilderWithFixer()
              .WithSourceCode("""
                record R(int x)
                {
                    private readonly int [|_x|] = x;

                    public int M() => _x;
                }
                """)
              .ShouldFixCodeWith(index: 1, """
                record R(int x)
                {
                    public int M() => x;
                }
                """)
              .ValidateAsync();
    }
}
#endif
