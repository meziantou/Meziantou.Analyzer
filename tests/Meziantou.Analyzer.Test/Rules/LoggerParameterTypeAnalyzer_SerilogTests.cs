using System.Diagnostics;
using Microsoft.CodeAnalysis;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.LoggerParameterTypeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class LoggerParameterTypeAnalyzer_SerilogTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddSerilog();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        return test;
    }

    [Fact]
    public Task SeriLog_Log_Information()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{Prop}", 1);
            Log.Information("{Prop}", {|MA0139:(int?)1|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_Exception()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information((System.Exception)null, "{Prop}", 1);
            Log.Information((System.Exception)null, "{Prop}", {|MA0139:(int?)1|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_Params()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{Prop}{Prop}{Prop}{Prop}", 1, 1, 1, 1);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_AtPrefix()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{@Prop}", 1);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_DollarPrefix()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{$Prop}", 1);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_AtPrefix_MultipleParams()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{@Prop1}{@Prop2}", 1, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop1;System.Int32
            Prop2;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_Information_AtPrefix_MixedParams()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Information("{Bar}{@Prop}", 1, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Bar;System.Int32
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_AtPrefix_MultipleParams()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Serilog.ILogger logger = null!;
            logger.Debug("{@Prop1}{@Prop2}", 1, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop1;System.Int32
            Prop2;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_AtPrefix_MixedParams()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Serilog.ILogger logger = null!;
            logger.Debug("{Bar}{@Prop}", 1, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Bar;System.Int32
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Enrich_WithProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            new LoggerConfiguration().Enrich.WithProperty("Prop", 0);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Enrich_WithProperty_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            new LoggerConfiguration().Enrich.WithProperty("Prop", {|MA0139:""|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_ForContext()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.ForContext("Prop", 0);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_Log_ForContext_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.ForContext("Prop", {|MA0139:""|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_ForContext()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Logger.ForContext("Prop", 0);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_ForContext_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Logger.ForContext("Prop", {|MA0139:""|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_ForContext_LogEventLevel()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Logger.ForContext(Serilog.Events.LogEventLevel.Warning, "Prop", 0);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task SeriLog_ILogger_ForContext_LogEventLevel_Invalid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Serilog;

            Log.Logger.ForContext(Serilog.Events.LogEventLevel.Warning,"Prop", {|MA0139:""|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }
}
