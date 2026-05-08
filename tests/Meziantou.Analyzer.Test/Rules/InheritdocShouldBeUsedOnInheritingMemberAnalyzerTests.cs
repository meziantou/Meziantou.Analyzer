using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class InheritdocShouldBeUsedOnInheritingMemberAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<InheritdocShouldBeUsedOnInheritingMemberAnalyzer>()
            .WithTargetFramework(TargetFramework.NetLatest);
    }

    [Fact]
    public async Task ReportDiagnostic_MethodIsNotOverrideOrInterfaceImplementation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// [|<inheritdoc />|]
                      public void M() { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_PropertyIsNotOverrideOrInterfaceImplementation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// [|<inheritdoc />|]
                      public int P { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_ConstructorIsNotOverrideOrInterfaceImplementation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// [|<inheritdoc />|]
                      public Sample() { }
                  }
                  """)
              .ValidateAsync();
    }

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task NoDiagnostic_WhenInheritdocIsOnTypeWithPrimaryConstructor()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  /// <inheritdoc />
                  public class Sample() { }
                  """)
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task NoDiagnostic_MethodIsOverride()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class BaseType
                  {
                      public virtual void M() { }
                  }

                  class Sample : BaseType
                  {
                      /// <inheritdoc />
                      public override void M() { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_MethodIsInterfaceImplementation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITest
                  {
                      void M();
                  }

                  class Sample : ITest
                  {
                      /// <inheritdoc />
                      public void M() { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_MethodIsExplicitInterfaceImplementation()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITest
                  {
                      void M();
                  }

                  class Sample : ITest
                  {
                      /// <inheritdoc />
                      void ITest.M() { }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenCrefIsPresent()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      /// <inheritdoc cref="object.ToString" />
                      public void M() { }
                  }
                  """)
              .ValidateAsync();
    }
}
