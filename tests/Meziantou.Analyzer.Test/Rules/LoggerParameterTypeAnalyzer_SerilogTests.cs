using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class LoggerParameterTypeAnalyzer_SerilogTests
{
    private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<LoggerParameterTypeAnalyzer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
            .WithTargetFramework(TargetFramework.Net8_0)
            .AddNuGetReference("Serilog", "3.1.1", "lib/net7.0");

    [Fact]
    public async Task SeriLog_Log_Information()
    {
        const string SourceCode = """
using Serilog;

Log.Information("{Prop}", 1);
Log.Information("{Prop}", [||](int?)1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Log_Information_Exception()
    {
        const string SourceCode = """
using Serilog;

Log.Information((System.Exception)null, "{Prop}", 1);
Log.Information((System.Exception)null, "{Prop}", [||](int?)1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Log_Information_Params()
    {
        const string SourceCode = """
using Serilog;

Log.Information("{Prop}{Prop}{Prop}{Prop}", 1, 1, 1, 1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }    

    [Fact]
    public async Task SeriLog_Log_Information_AtPrefix()
    {
        const string SourceCode = """
using Serilog;

Log.Information("{@Prop}", 1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task SeriLog_Log_Information_DollarPrefix()
    {
        const string SourceCode = """
using Serilog;

Log.Information("{$Prop}", 1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Enrich_WithProperty()
    {
        const string SourceCode = """
using Serilog;

new LoggerConfiguration().Enrich.WithProperty("Prop", 0);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Enrich_WithProperty_Invalid()
    {
        const string SourceCode = """
using Serilog;

new LoggerConfiguration().Enrich.WithProperty("Prop", [||]"");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Log_ForContext()
    {
        const string SourceCode = """
using Serilog;

Log.ForContext("Prop", 0);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_Log_ForContext_Invalid()
    {
        const string SourceCode = """
using Serilog;

Log.ForContext("Prop", [||]"");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_ILogger_ForContext()
    {
        const string SourceCode = """
using Serilog;

Log.Logger.ForContext("Prop", 0);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_ILogger_ForContext_Invalid()
    {
        const string SourceCode = """
using Serilog;

Log.Logger.ForContext("Prop", [||]"");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_ILogger_ForContext_LogEventLevel()
    {
        const string SourceCode = """
using Serilog;

Log.Logger.ForContext(Serilog.Events.LogEventLevel.Warning, "Prop", 0);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task SeriLog_ILogger_ForContext_LogEventLevel_Invalid()
    {
        const string SourceCode = """
using Serilog;

Log.Logger.ForContext(Serilog.Events.LogEventLevel.Warning,"Prop", [||]"");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }
}
