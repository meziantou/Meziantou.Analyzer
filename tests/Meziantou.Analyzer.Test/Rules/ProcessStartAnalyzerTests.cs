using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ProcessStartAnalyzerTests
{
    [Fact]
    public async Task Process_start_should_not_report_when_use_shell_execute_is_set_to_false()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          Process.Start(new ProcessStartInfo { UseShellExecute = false });
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0161")
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_not_report_when_use_shell_execute_is_set_to_true()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          Process.Start(new ProcessStartInfo { UseShellExecute = true });
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0161")
            .WithSourceCode(SourceCode)
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_not_set()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo();
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0161")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo")
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_set_to_true_and_output_redirected()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          const bool useShellExecute = true;
                                          var processStartInfo = [||]new ProcessStartInfo()
                                          {
                                              RedirectStandardOutput = true,
                                              UseShellExecute = useShellExecute,
                                          };
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0163")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Set UseShellExecute to false when redirecting standard input or output")
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_not_set_and_output_redirected()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo()
                                          {
                                              RedirectStandardOutput = true,
                                          };
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0163")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Set UseShellExecute to false when redirecting standard input or output")
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_not_set_and_error_redirected()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo()
                                          {
                                              RedirectStandardError = true,
                                          };
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0163")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Set UseShellExecute to false when redirecting standard input or output")
            .ValidateAsync();
    }


    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_not_set_and_input_redirected()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo()
                                          {
                                              RedirectStandardInput = true,
                                              UseShellExecute = true,
                                          };
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0163")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Set UseShellExecute to false when redirecting standard input or output")
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_false_positives()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo();
                                          processStartInfo.UseShellExecute = false;
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0161")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo")
            .ValidateAsync();
    }

    [Fact]
    public async Task Process_start_should_report_when_use_shell_execute_is_not_set_2()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          var processStartInfo = [||]new ProcessStartInfo()
                                          {
                                              FileName = "notepad",
                                          };
                                          Process.Start(processStartInfo);
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0161")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("UseShellExecute must be explicitly set when initializing a ProcessStartInfo")
            .ValidateAsync();
    }


    [Fact]
    public async Task Process_start_should_report_when_using_overload_with_no_process_start_info()
    {
        const string SourceCode = """
                                  using System.Diagnostics;

                                  class TypeName
                                  {
                                      public void Test()
                                      {
                                          [||]Process.Start("notepad");
                                      }
                                  }
                                  """;
        await CreateProjectBuilder("MA0162")
            .WithSourceCode(SourceCode)
            .ShouldReportDiagnosticWithMessage("Use an overload of Process.Start that has a ProcessStartInfo parameter")
            .ValidateAsync();
    }

    private static ProjectBuilder CreateProjectBuilder(string id) => new ProjectBuilder().WithAnalyzer<ProcessStartAnalyzer>(id);
}
