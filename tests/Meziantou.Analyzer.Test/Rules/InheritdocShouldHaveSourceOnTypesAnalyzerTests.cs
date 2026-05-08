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

    [Fact]
    public async Task NoDiagnostic_WhenBaseTypeIsPresent()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class BaseClass
                  {
                  }

                  /// <inheritdoc />
                  class Sample : BaseClass
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenCrefIsPresent()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <inheritdoc cref="object" />
                  class Sample
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenInterfaceInheritsAnotherInterface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface IBase
                  {
                  }

                  /// <inheritdoc />
                  interface IChild : IBase
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenRecordInheritsBaseRecord()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  record BaseRecord;

                  /// <inheritdoc />
                  record Sample : BaseRecord;
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenStructImplementsInterface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITest
                  {
                  }

                  /// <inheritdoc />
                  struct Sample : ITest
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenRecordStructImplementsInterface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITest
                  {
                  }

                  /// <inheritdoc />
                  record struct Sample : ITest;
                  """)
              .ValidateAsync();
    }
}
