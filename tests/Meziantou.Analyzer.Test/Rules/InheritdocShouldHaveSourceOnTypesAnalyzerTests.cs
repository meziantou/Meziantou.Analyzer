using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldHaveSourceOnTypesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<InheritdocShouldHaveSourceOnTypesAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ReportDiagnostic_MA0199_WhenNoBaseTypeAndNoDeclaredInterface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0199:<inheritdoc />|}
                  class Sample
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_MA0199_WhenInterfaceHasNoBaseInterface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0199:<inheritdoc />|}
                  interface ITest
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_ForEachPartialDeclaration()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// {|MA0199:<inheritdoc />|}
                  partial class Sample
                  {
                  }

                  /// {|MA0199:<inheritdoc />|}
                  partial class Sample
                  {
                  }
                  """)
              .ValidateAsync();
    }
}
