using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RemoveUnnecessaryPartialModifierAnalyzer,
    Meziantou.Analyzer.Rules.RemoveUnnecessaryPartialModifierFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RemoveUnnecessaryPartialModifierAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task PartialClass_WithSingleDeclaration_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [|partial|] class Sample
            {
            }
            """;
        test.FixedCode = """
            class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithSingleDeclaration_PreserveComments_Keyword_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            /*sample*/[|partial|] class Sample
            {
            }
            """;
        test.FixedCode = """
            /*sample*/class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithSingleDeclaration_PreserveComments_Modifier_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            static /*sample*/[|partial|] class Sample
            {
            }
            """;
        test.FixedCode = """
            static /*sample*/class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithOtherModifiers_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed [|partial|] class Sample
            {
            }
            """;
        test.FixedCode = """
            public sealed class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialRecord_WithSingleDeclaration_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [|partial|] record Sample;
            """;
        test.FixedCode = """
            record Sample;
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialStruct_WithSingleDeclaration_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [|partial|] struct Sample
            {
            }
            """;
        test.FixedCode = """
            struct Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialInterface_WithSingleDeclaration_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [|partial|] interface ISample
            {
            }
            """;
        test.FixedCode = """
            interface ISample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithMultipleDeclarations_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class Sample
            {
            }

            partial class Sample
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithPartialMethod_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            [|partial|] class Sample
            {
                partial void M();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_WithNestedPartialType_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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
        test.FixedCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_InheritingFromWpfUserControl_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_InheritingFromWpfPage_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_InheritingFromWpfApplication_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task PartialClass_InheritingFromWpfWindow_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace System.Windows
            {
                public class Window
                {
                }
            }
            partial class Sample : System.Windows.Window
            {
            }
            """;

        return test.RunAsync();
    }
}
