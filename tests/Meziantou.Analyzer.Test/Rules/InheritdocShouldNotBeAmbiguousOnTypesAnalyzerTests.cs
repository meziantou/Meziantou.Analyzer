using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldNotBeAmbiguousOnTypesAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<InheritdocShouldNotBeAmbiguousOnTypesAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    private static ProjectBuilder CreateProjectBuilderWithCodeFixProvider()
    {
        return CreateProjectBuilder()
            .WithCodeFixProvider<InheritdocShouldNotBeUsedOnTypesFixer>();
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

                  /// <inheritdoc cref="T:IInterface1" />
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

                  /// <inheritdoc cref="T:IInterface2" />
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

                  /// <inheritdoc cref="T:IInterface2"></inheritdoc>
                  class Sample : IInterface1, IInterface2
                  {
                  }
                  """)
              .ValidateAsync();
    }
}
