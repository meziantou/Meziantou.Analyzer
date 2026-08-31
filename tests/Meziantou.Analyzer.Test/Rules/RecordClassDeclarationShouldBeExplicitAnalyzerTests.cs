using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.RecordClassDeclarationShouldBeExplicitAnalyzer,
    Meziantou.Analyzer.Rules.RecordClassDeclarationShouldBeExplicitFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class RecordClassDeclarationShouldBeExplicitAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task ImplicitRecordClass_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0174:record|} Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_WithModifiers_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed {|MA0174:record|} Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record class Target { }
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
    public Task ImplicitRecordClass_WithParameters_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0174:record|} Target(int Id) { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitRecordClass_WithParameters_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public record class Target(int Id) { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_Generic_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0174:record|} Target<T> { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_InNamespace_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            namespace MyNamespace
            {
                public {|MA0174:record|} Target { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_WithInheritance_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public abstract {|MA0174:record|} BaseRecord { }
            public {|MA0174:record|} Target : BaseRecord { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RxplicitRecordClass_WithInheritance_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            public abstract record class BaseRecord { }
            public record class Target : BaseRecord { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_CodeFix_ShouldAddClassKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0174:record|} Target { }
            """;
        test.FixedCode = """
            public record class Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_WithModifiers_CodeFix_ShouldAddClassKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed {|MA0174:record|} Target { }
            """;
        test.FixedCode = """
            public sealed record class Target { }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitRecordClass_WithParameters_CodeFix_ShouldAddClassKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            public {|MA0174:record|} Target(int Id) { }
            """;
        test.FixedCode = """
            public record class Target(int Id) { }
            """;

        return test.RunAsync();
    }
}
