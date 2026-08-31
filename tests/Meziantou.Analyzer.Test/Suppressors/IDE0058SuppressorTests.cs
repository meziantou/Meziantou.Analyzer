#if ROSLYN_4_10_OR_GREATER
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Suppressors.IDE0058Suppressor>;
using WithoutSuppressorTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;

namespace Meziantou.Analyzer.Test.Suppressors;

public sealed class IDE0058SuppressorTests
{
    /// <summary>
    /// The diagnostic IDE0058 reports on the expression marked with <c>{|#0:code|}</c>,
    /// which the suppressor is expected to suppress or not.
    /// </summary>
    private static DiagnosticResult IDE0058(bool suppressed) =>
        new DiagnosticResult("IDE0058", DiagnosticSeverity.Hidden).WithLocation(0).WithIsSuppressed(suppressed);

    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.AddMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers("IDE0058");
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task IDE0058IsReported()
    {
        // Ensure the diagnostic is reported without the suppressor
        var test = new WithoutSuppressorTest();
        test.AddMicrosoftCodeAnalysisCSharpCodeStyleAnalyzers("IDE0058");
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            static void A()
            {
                {|IDE0058:new System.Text.StringBuilder().Append("Hello")|};
                {|IDE0058:System.IO.Directory.CreateDirectory("dir")|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StringBuilder_Append()
    {
        var test = CreateTest();
        test.TestCode = """
            static void A()
            {
                var sb = new System.Text.StringBuilder();
                {|#0:sb.Append("Hello")|};
                System.Console.WriteLine(sb.ToString());
            }
            """;
        test.ExpectedDiagnostics.Add(IDE0058(suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task Directory_CreateDirectory()
    {
        var test = CreateTest();
        test.TestCode = """
            static void A()
            {
                {|#0:System.IO.Directory.CreateDirectory("dir")|};
            }
            """;
        test.ExpectedDiagnostics.Add(IDE0058(suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task System_IO_Stream_Seek_0_End()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            static void A()
            {
                Stream stream = null;
                {|#0:stream.Seek(0, SeekOrigin.End)|};
            }
            """;
        test.ExpectedDiagnostics.Add(IDE0058(suppressed: false));

        return test.RunAsync();
    }

    [Fact]
    public Task System_IO_Stream_Seek_0_Begin()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            static void A()
            {
                Stream stream = null;
                {|#0:stream.Seek(0, SeekOrigin.Begin)|};
            }
            """;
        test.ExpectedDiagnostics.Add(IDE0058(suppressed: true));

        return test.RunAsync();
    }

    [Fact]
    public Task System_Collections_Generic_HashSet_Add()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            static void A()
            {
                System.Collections.Generic.HashSet<int> a = null;
                {|#0:a.Add(0)|};
            }
            """;
        test.ExpectedDiagnostics.Add(IDE0058(suppressed: true));

        return test.RunAsync();
    }
}
#endif
