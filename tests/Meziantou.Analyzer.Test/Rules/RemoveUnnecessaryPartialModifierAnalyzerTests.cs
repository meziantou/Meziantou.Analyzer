using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryPartialModifierAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<RemoveUnnecessaryPartialModifierAnalyzer>()
            .WithCodeFixProvider<RemoveUnnecessaryPartialModifierFixer>();
    }

    [Fact]
    public async Task PartialClass_WithSingleDeclaration_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|partial|] class Sample
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
    public async Task PartialClass_WithSingleDeclaration_PreserveComments_Keyword_ReportsDiagnostic()
    {
        const string SourceCode = """
            /*sample*/[|partial|] class Sample
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
    public async Task PartialClass_WithSingleDeclaration_PreserveComments_Modifier_ReportsDiagnostic()
    {
        const string SourceCode = """
            static /*sample*/[|partial|] class Sample
            {
            }
            """;

        const string CodeFix = """
            static /*sample*/class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialClass_WithOtherModifiers_ReportsDiagnostic()
    {
        const string SourceCode = """
            public sealed [|partial|] class Sample
            {
            }
            """;

        const string CodeFix = """
            public sealed class Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialRecord_WithSingleDeclaration_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|partial|] record Sample;
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
    public async Task PartialStruct_WithSingleDeclaration_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|partial|] struct Sample
            {
            }
            """;

        const string CodeFix = """
            struct Sample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialInterface_WithSingleDeclaration_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|partial|] interface ISample
            {
            }
            """;

        const string CodeFix = """
            interface ISample
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ShouldFixCodeWith(CodeFix)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialClass_WithMultipleDeclarations_NoDiagnostic()
    {
        const string SourceCode = """
            partial class Sample
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
    public async Task PartialClass_WithPartialMethod_NoDiagnostic()
    {
        const string SourceCode = """
            [|partial|] class Sample
            {
                partial void M();
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialClass_WithNestedPartialType_ReportsDiagnostic()
    {
        const string SourceCode = """
            [|partial|] class Sample
            {
                partial class Nested
                {
                }

                partial class Nested
                {
                }
            }
            """;

        const string CodeFix = """
            class Sample
            {
                partial class Nested
                {
                }

                partial class Nested
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
    public async Task PartialClass_InheritingFromWpfUserControl_NoDiagnostic()
    {
        const string SourceCode = """
            namespace System.Windows.Controls
            {
                public class UserControl
                {
                }
            }

            partial class Sample : System.Windows.Controls.UserControl
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialClass_InheritingFromWpfPage_NoDiagnostic()
    {
        const string SourceCode = """
            namespace System.Windows.Controls
            {
                public class Page
                {
                }
            }

            partial class Sample : System.Windows.Controls.Page
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task PartialClass_InheritingFromWpfApplication_NoDiagnostic()
    {
        const string SourceCode = """
            namespace System.Windows
            {
                public class Application
                {
                }
            }

            partial class Sample : System.Windows.Application
            {
            }
            """;

        await CreateProjectBuilder()
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }
}
