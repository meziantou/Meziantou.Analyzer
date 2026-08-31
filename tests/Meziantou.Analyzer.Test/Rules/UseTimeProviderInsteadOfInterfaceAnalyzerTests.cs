using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.UseTimeProviderInsteadOfInterfaceAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTimeProviderInsteadOfInterfaceAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task Interface_UtcNowProperty_DateTime_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTime UtcNow { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_NowProperty_DateTimeOffset_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTimeOffset Now { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_BothNowAndUtcNow_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTime Now { get; }
                System.DateTime UtcNow { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_GetNowMethod_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTime GetNow();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_GetUtcNowMethod_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTimeOffset GetUtcNow();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_MixedProperties_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeService|}
            {
                System.DateTime GetNow();
                System.DateTimeOffset GetUtcNow();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_EmptyInterface_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface IEmpty
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_OtherMembers_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITimeProvider
            {
                System.DateTime UtcNow { get; }
                void DoSomething();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_WrongReturnType_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITimeProvider
            {
                string UtcNow { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_MethodWithParameters_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITimeProvider
            {
                System.DateTime GetNow(string timeZone);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_CurrentTimeProperty_ReportsDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface {|MA0188:ITimeProvider|}
            {
                System.DateTime CurrentTime { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_WrongName_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITimeProvider
            {
                System.DateTime GetCurrentTime();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Class_NotAnInterface_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class TimeProvider
            {
                public System.DateTime UtcNow { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_StaticMember_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITimeProvider
            {
                static System.DateTime UtcNow { get; }
            }
            """;

        return test.RunAsync();
    }
}
