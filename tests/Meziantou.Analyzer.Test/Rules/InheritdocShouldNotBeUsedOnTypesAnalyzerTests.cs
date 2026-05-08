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

    private static ProjectBuilder CreateProjectBuilderWithCodeFixProvider()
    {
        return CreateProjectBuilder()
            .WithCodeFixProvider<InheritdocShouldNotBeUsedOnTypesFixer>();
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
    public async Task ReportDiagnostic_MA0198_WhenMultipleDeclaredInterfacesArePresentAndNoBaseType()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// {|MA0198:<inheritdoc />|}
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ValidateAsync();
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
    public async Task CodeFix_MA0198_EmptyElement_FirstInterface()
    {
        await CreateProjectBuilderWithCodeFixProvider()
              .WithSourceCode("""
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// {|MA0198:<inheritdoc />|}
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ShouldFixCodeWith(index: 0, """
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// <inheritdoc cref="IInterface1" />
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_MA0198_EmptyElement_SecondInterface()
    {
        await CreateProjectBuilderWithCodeFixProvider()
              .WithSourceCode("""
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// {|MA0198:<inheritdoc />|}
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ShouldFixCodeWith(index: 1, """
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// <inheritdoc cref="IInterface2" />
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task CodeFix_MA0198_XmlElement()
    {
        await CreateProjectBuilderWithCodeFixProvider()
              .WithSourceCode("""
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// {|MA0198:<inheritdoc>|}</inheritdoc>
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ShouldFixCodeWith(index: 1, """
                  interface IInterface1
                  {
                  }

                  interface IInterface2
                  {
                  }

                  /// <inheritdoc cref="IInterface2"></inheritdoc>
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ValidateAsync();
    }
}
