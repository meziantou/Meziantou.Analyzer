using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTimeProviderInsteadOfInterfaceAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<UseTimeProviderInsteadOfInterfaceAnalyzer>();
    }

    [Fact]
    public async Task Interface_UtcNowProperty_DateTime_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeProvider|]
                  {
                      System.DateTime UtcNow { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_NowProperty_DateTimeOffset_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeProvider|]
                  {
                      System.DateTimeOffset Now { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_BothNowAndUtcNow_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeProvider|]
                  {
                      System.DateTime Now { get; }
                      System.DateTime UtcNow { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_GetNowMethod_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeProvider|]
                  {
                      System.DateTime GetNow();
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_GetUtcNowMethod_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeProvider|]
                  {
                      System.DateTimeOffset GetUtcNow();
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_MixedProperties_ReportsDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface [|ITimeService|]
                  {
                      System.DateTime GetNow();
                      System.DateTimeOffset GetUtcNow();
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_EmptyInterface_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface IEmpty
                  {
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_OtherMembers_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITimeProvider
                  {
                      System.DateTime UtcNow { get; }
                      void DoSomething();
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_WrongReturnType_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITimeProvider
                  {
                      string UtcNow { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_MethodWithParameters_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITimeProvider
                  {
                      System.DateTime GetNow(string timeZone);
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Interface_WrongName_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  interface ITimeProvider
                  {
                      System.DateTime CurrentTime { get; }
                  }
                  """)
              .ValidateAsync();
    }

    [Fact]
    public async Task Class_NotAnInterface_NoDiagnostic()
    {
        await CreateProjectBuilder()
              .WithSourceCode("""
                  class TimeProvider
                  {
                      public System.DateTime UtcNow { get; }
                  }
                  """)
              .ValidateAsync();
    }
}
