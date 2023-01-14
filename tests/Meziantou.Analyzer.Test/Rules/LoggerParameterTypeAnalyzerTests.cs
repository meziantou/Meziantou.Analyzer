using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class LoggerParameterTypeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<LoggerParameterTypeAnalyzer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
            .WithTargetFramework(TargetFramework.Net7_0)
            .AddNuGetReference("Microsoft.Extensions.Logging.Abstractions", "7.0.0", "lib/net7.0");

    [Fact]
    public async Task BeginScope_InvalidParameterType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.BeginScope("{Prop} {Name}", [||]1, 2);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
Name;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task BeginScope_InvalidParameterType_XmlCommentId()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.BeginScope("{Prop} {Name} {Name}", [||]1, 2, (int?)null);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
Count;T:System.Nullable{System.Int32}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task LogInformation_InvalidParameterType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop} {Name}", [||]1, 2);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
Name;System.Int32
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task LogInformation_ValidParameterType2()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop} {Name} {Name}", "test", 2, 3L);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
# This is a comment
Prop;System.String
Name;System.Int32;System.Int64
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task LogInformation_NoConfigurationFile()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop} {Name}", "test", 2, 3L);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LogInformation_EmptyConfigurationFile()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop} {Name} {Name}", "test", 2, 3L);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", "")
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_Define_InvalidParameterType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

LoggerMessage.Define<[|int|], string>(LogLevel.Information, new EventId(0), "{Prop} {Name}");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
Name;System.String
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_DefineScope_InvalidParameterType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

LoggerMessage.DefineScope<[|int|], string>("{Prop} {Name}");
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
Name;System.String
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Configuration_UnknownParameterType()
    {
        await CreateProjectBuilder()
              .WithSourceCode("")
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .AddAdditionalFile("LoggerParameterTypes.txt", "Prop;int")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0125", Locations = new[] { new DiagnosticResultLocation("LoggerParameterTypes.txt", 1, 1, 1, 1) } })
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Configuration_CommentIdToMember()
    {
        await CreateProjectBuilder()
              .WithSourceCode("")
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .AddAdditionalFile("LoggerParameterTypes.txt", "Prop;M:System.Int32.MaxValue")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0125", Locations = new[] { new DiagnosticResultLocation("LoggerParameterTypes.txt", 1, 1, 1, 1) } })
              .ValidateAsync();
    }

    [Fact]
    public async Task Configuration_DuplicateParameterName()
    {
        await CreateProjectBuilder()
              .WithSourceCode("")
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
              .AddAdditionalFile("LoggerParameterTypes.1.txt", "Prop;System.String")
              .AddAdditionalFile("LoggerParameterTypes.2.txt", "New;System.String\nProp;System.String")
              .ShouldReportDiagnostic(new DiagnosticResult { Id = "MA0126", Locations = new[] { new DiagnosticResultLocation("LoggerParameterTypes.2.txt", 2, 1, 2, 1) } })
              .ValidateAsync();
    }
}
