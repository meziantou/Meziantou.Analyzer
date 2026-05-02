using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotUseNotYetInitializedStaticFieldAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotUseNotYetInitializedStaticFieldAnalyzer>();
    }

    [Fact]
    public async Task ReportDiagnostic_WhenReferencingLaterFieldInSamePart()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static readonly bool[] Values = new[] { [|P1|], [|P2|] };
                      private static readonly bool P1 = true;
                      private static readonly bool P2 = false;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencingEarlierFieldInSamePart()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static readonly bool P1 = true;
                      private static readonly bool P2 = false;
                      private static readonly bool[] Values = new[] { P1, P2 };
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task ReportDiagnostic_WhenReferencingFieldFromAnotherPartialDeclaration()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  partial class Sample
                  {
                      private static readonly bool P1 = true;
                  }

                  partial class Sample
                  {
                      private static readonly bool[] Values = new[] { [|P1|] };
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenPartialDeclarationsOnlyReferenceEarlierFieldsInSamePart()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  partial class Sample
                  {
                      private static readonly int P1 = 1;
                      private static readonly int[] Values1 = new[] { P1 };
                  }

                  partial class Sample
                  {
                      private static readonly int P2 = 2;
                      private static readonly int[] Values2 = new[] { P2 };
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencingFieldFromAnotherPartialDeclarationWithoutInitializer()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  partial class Sample
                  {
                      private static readonly int Other;
                  }

                  partial class Sample
                  {
                      private static readonly int Value = Other;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferencedFieldHasNoInitializer()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static readonly int Value = Other;
                      private static readonly int Other;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferenceIsInNameof()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static readonly string Value = nameof(Other);
                      private static readonly int Other = 42;
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task NoDiagnostic_WhenReferenceIsInLambda()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class Sample
                  {
                      private static readonly System.Func<int> ValueFactory = () => Other;
                      private static readonly int Other = 42;
                  }
                  """)
              .ValidateAsync();
    }
}
