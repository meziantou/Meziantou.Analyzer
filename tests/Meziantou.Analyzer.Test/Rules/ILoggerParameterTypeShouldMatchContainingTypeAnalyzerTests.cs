using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ILoggerParameterTypeShouldMatchContainingTypeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ILoggerParameterTypeShouldMatchContainingTypeAnalyzer>(id: "MA0180")
            .WithCodeFixProvider<ILoggerParameterTypeShouldMatchContainingTypeFixer>()
            .WithTargetFramework(TargetFramework.Net8_0)
            .AddNuGetReference("Microsoft.Extensions.Logging.Abstractions", "8.0.0", "lib/net8.0");
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task PrimaryConstructor_Mismatch_ShouldReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A([|ILogger<B>|] logger)
            {
            }

            class B
            {
            }
            """;

        var fix = """
            using Microsoft.Extensions.Logging;

            class A(ILogger<A> logger)
            {
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task RegularConstructor_Mismatch_ShouldReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A
            {
                public A([|ILogger<B>|] logger)
                {
                }
            }

            class B
            {
            }
            """;

        var fix = """
            using Microsoft.Extensions.Logging;

            class A
            {
                public A(ILogger<A> logger)
                {
                }
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task PrimaryConstructor_Match_ShouldNotReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A(ILogger<A> logger)
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task RegularConstructor_Match_ShouldNotReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A
            {
                public A(ILogger<A> logger)
                {
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task NonGenericILogger_ShouldNotReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A(ILogger logger)
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task AbstractClass_ShouldNotReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            abstract class A(ILogger<B> logger)
            {
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task Interface_ShouldNotReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            interface IA
            {
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task MultipleConstructors_Mismatch_ShouldReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A
            {
                public A([|ILogger<B>|] logger)
                {
                }

                public A(string name, [|ILogger<B>|] logger)
                {
                }
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task NestedClass_Mismatch_ShouldReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class Outer
            {
                class Inner([|ILogger<Outer>|] logger)
                {
                }
            }
            """;

        var fix = """
            using Microsoft.Extensions.Logging;

            class Outer
            {
                class Inner(ILogger<Inner> logger)
                {
                }
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }
#endif

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task GenericClass_Mismatch_ShouldReportDiagnostic()
    {
        var sourceCode = """
            using Microsoft.Extensions.Logging;

            class A<T>([|ILogger<B>|] logger)
            {
            }

            class B
            {
            }
            """;

        var fix = """
            using Microsoft.Extensions.Logging;

            class A<T>(ILogger<A<T>> logger)
            {
            }

            class B
            {
            }
            """;

        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ShouldFixCodeWith(fix)
              .ValidateAsync();
    }
#endif
}
