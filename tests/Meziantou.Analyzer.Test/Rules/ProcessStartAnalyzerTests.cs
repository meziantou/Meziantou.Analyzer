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

    [Fact]
    public Task Process_start_should_report_false_positives()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Diagnostics;

            class TypeName
            {
                public void Test()
                {
                    var processStartInfo = {|#0:new ProcessStartInfo()|};
                    processStartInfo.UseShellExecute = false;
                    Process.Start(processStartInfo);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0161", DiagnosticSeverity.Info).WithLocation(0).WithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo"));

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
