using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RecordClassDeclarationShouldBeImplicitAnalyzer,
    Meziantou.Analyzer.Rules.RecordClassDeclarationShouldBeImplicitFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RecordClassDeclarationShouldBeImplicitAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ExplicitRecordClass_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record {|MA0175:class|} Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_WithModifiers_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed record {|MA0175:class|} Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordStruct_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record struct Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegularClass_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RegularStruct_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public struct Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_WithParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record {|MA0175:class|} Target(int Id) { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_WithParameters_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record Target(int Id) { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_Generic_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record {|MA0175:class|} Target<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_InNamespace_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace MyNamespace
            {
                public record {|MA0175:class|} Target { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_WithInheritance_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public abstract record BaseRecord { }
            public record {|MA0175:class|} Target : BaseRecord { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_ExplicitRecordClass()
    {
        var test = CreateTest();
        test.TestCode = """
            public record {|MA0175:class|} Target { }
            """;
        test.FixedCode = """
            public record Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_ExplicitRecordClass_WithModifiers()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed record {|MA0175:class|} Target { }
            """;
        test.FixedCode = """
            public sealed record Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Fix_ExplicitRecordClass_WithParameters()
    {
        var test = CreateTest();
        test.TestCode = """
            public record {|MA0175:class|} Target(int Id) { }
            """;
        test.FixedCode = """
            public record Target(int Id) { }
            """;

        return test.RunAsync();
    }
}
