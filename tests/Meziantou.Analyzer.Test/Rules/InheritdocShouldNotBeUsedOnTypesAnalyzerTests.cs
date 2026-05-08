using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldNotBeUsedOnTypesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<InheritdocShouldNotBeUsedOnTypesAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ReportDiagnostic_MA0197_WhenBaseTypeIsPresent()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class BaseType
                  {
                  }

                  /// {|MA0197:<inheritdoc />|}
                  class Sample : BaseType
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_MA0197_WhenSingleDeclaredInterfaceIsPresent()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITest
                  {
                  }

                  /// {|MA0197:<inheritdoc />|}
                  class Sample : ITest
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_MA0197_WhenDeclaredInterfaceInheritsMultipleInterfaces()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  interface ICompositeInterface : IInterface1, IInterface2
                  {
                  }

                  /// {|MA0197:<inheritdoc />|}
                  class Sample : ICompositeInterface
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
    public async Task NoDiagnostic_WhenCrefIsPresentOnXmlElement()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <inheritdoc cref="object"></inheritdoc>
                  class Sample
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenUsedOnMember()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// <inheritdoc />
                      public override string ToString() => base.ToString();
                  }
                  """)
              .ValidateAsync();
    }
}
