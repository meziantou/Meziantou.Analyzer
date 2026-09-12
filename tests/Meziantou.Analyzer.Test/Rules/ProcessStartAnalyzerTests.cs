using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ProcessStartAnalyzer,
    Meziantou.Analyzer.Rules.UseShellExecuteMustBeSetFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ProcessStartAnalyzerTests
{
    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_to_false()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    Process.Start(new ProcessStartInfo { UseShellExecute = false });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_to_true()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    Process.Start(new ProcessStartInfo { UseShellExecute = true });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_fix_when_use_shell_execute_is_not_set()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|MA0161:new ProcessStartInfo()|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.FixedCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { UseShellExecute = false };
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_fix_when_use_shell_execute_is_not_set_by_setting_it_to_true()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|MA0161:new ProcessStartInfo()|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.CodeActionIndex = 1;
        test.CodeActionEquivalenceKey = "Set UseShellExecute to true";
        test.FixedCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { UseShellExecute = true };
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_set_to_true_and_output_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    const bool useShellExecute = true;
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = useShellExecute,
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_and_output_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_and_error_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        RedirectStandardError = true,
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_and_input_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        RedirectStandardInput = true,
                        UseShellExecute = true,
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("true")]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_after_the_creation(string value)
    {
        var test = new CodeFixTest();
        test.TestCode = $$"""
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo();
                    processStartInfo.UseShellExecute = {{value}};
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_after_the_creation_on_a_field()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                private ProcessStartInfo _processStartInfo;

                public void Test()
                {
                    _processStartInfo = new ProcessStartInfo();
                    _processStartInfo.UseShellExecute = false;
                    Process.Start(_processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_after_the_creation_on_an_existing_variable()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    ProcessStartInfo processStartInfo;
                    processStartInfo = new ProcessStartInfo();
                    processStartInfo.UseShellExecute = false;
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_to_a_non_constant_value_after_the_creation()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test(bool useShellExecute)
                {
                    var processStartInfo = new ProcessStartInfo();
                    processStartInfo.UseShellExecute = useShellExecute;
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_set_on_another_variable()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()|};
                    var other = new ProcessStartInfo();
                    other.UseShellExecute = false;
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_only_set_before_the_creation()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { UseShellExecute = false };
                    processStartInfo.UseShellExecute = false;
                    processStartInfo = {|#0:new ProcessStartInfo()|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_output_is_redirected_after_the_creation()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()|};
                    processStartInfo.RedirectStandardOutput = true;
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_output_is_redirected_after_the_creation_and_use_shell_execute_is_true()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo() { UseShellExecute = true }|};
                    processStartInfo.RedirectStandardInput = true;
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_set_to_true_after_the_creation_and_output_is_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo() { RedirectStandardError = true }|};
                    processStartInfo.UseShellExecute = true;
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_to_false_after_the_creation_and_output_is_redirected()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true };
                    processStartInfo.UseShellExecute = false;
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_the_redirection_is_disabled_after_the_creation()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true, UseShellExecute = true };
                    processStartInfo.RedirectStandardOutput = false;
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_set_to_false_after_the_process_is_started()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = true,
                    }|};
                    Process.Start(processStartInfo);
                    processStartInfo.UseShellExecute = false;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_the_redirection_is_disabled_after_the_process_is_started()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo() { RedirectStandardOutput = true, UseShellExecute = true }|};
                    Process.Start(processStartInfo);
                    processStartInfo.RedirectStandardOutput = false;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_the_redirection_is_enabled_after_the_process_is_started()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()|};
                    Process.Start(processStartInfo);
                    processStartInfo.RedirectStandardOutput = true;
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_only_set_to_false_in_a_conditional_branch()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test(bool condition)
                {
                    var processStartInfo = {|#0:new ProcessStartInfo() { RedirectStandardOutput = true, UseShellExecute = true }|};
                    if (condition)
                    {
                        processStartInfo.UseShellExecute = false;
                    }

                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_the_redirection_is_enabled_in_a_conditional_branch()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test(bool condition)
                {
                    var processStartInfo = {|#0:new ProcessStartInfo() { UseShellExecute = true }|};
                    if (condition)
                    {
                        processStartInfo.RedirectStandardOutput = true;
                    }

                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0163", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Set UseShellExecute to false when redirecting standard input or output"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_the_process_is_started_in_the_branch_that_sets_use_shell_execute()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test(bool condition)
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true };
                    if (condition)
                    {
                        processStartInfo.UseShellExecute = false;
                        Process.Start(processStartInfo);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_the_process_is_started_in_a_loop_after_use_shell_execute_is_set()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test(bool condition)
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true };
                    while (condition)
                    {
                        processStartInfo.UseShellExecute = false;
                        Process.Start(processStartInfo);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_in_a_local_function()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true };
                    Configure();
                    Process.Start(processStartInfo);

                    void Configure() => processStartInfo.UseShellExecute = false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_not_report_when_use_shell_execute_is_set_after_the_start_info_is_assigned_to_a_process()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo() { RedirectStandardOutput = true };
                    using var process = new Process() { StartInfo = processStartInfo };
                    processStartInfo.UseShellExecute = false;
                    process.Start();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_2()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()
                    {
                        FileName = "notepad",
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_fix_when_use_shell_execute_is_not_set_and_initializer_exists()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|MA0161:new ProcessStartInfo()
                    {
                        FileName = "notepad",
                    }|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.FixedCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = new ProcessStartInfo()
                    {
                        FileName = "notepad",
                        UseShellExecute = false,
                    };
                    Process.Start(processStartInfo);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_3()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo("notepad")|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_use_shell_execute_is_not_set_4()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo("notepad", string.Empty)|};
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_using_overload_with_no_process_start_info()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    {|#0:Process.Start("notepad")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0162", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of Process.Start that has a ProcessStartInfo parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task Process_start_should_report_when_using_overload_with_no_process_start_info_2()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    {|#0:Process.Start("notepad", "file.txt")|};
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0162", DiagnosticSeverity.Info).WithLocation(0).WithMessage("Use an overload of Process.Start that has a ProcessStartInfo parameter"));

        return test.RunAsync();
    }
}
