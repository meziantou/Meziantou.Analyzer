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
    public async Task ReportDiagnostic_Class()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// [|<inheritdoc />|]
                  class Sample
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
    public async Task ReportDiagnostic_Interface()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// [|<inheritdoc />|]
                  interface ITest
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

    [Fact]
    public async Task ReportDiagnostic_ForEachPartialDeclaration()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// [|<inheritdoc />|]
                  partial class Sample
                  {
                  }

                  /// [|<inheritdoc />|]
                  partial class Sample
                  {
                  }
                  """)
              .ValidateAsync();
    }
}
