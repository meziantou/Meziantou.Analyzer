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
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
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
logger.BeginScope([||][||]"{Prop} {Name} {Name}", [||]1, 2, (int?)null);
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

#if ROSLYN_4_2_OR_GREATER
    [Fact]
    public async Task Logger_LogTrace_InvalidParameterType_FormattableString()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation($"{{Prop}} {2}", [|2|]);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
""")
              .ValidateAsync();
    }
#endif

    [Fact]
    public async Task Logger_LogTrace_InvalidParameterType_StringConcat()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
var a = "test";
logger.LogInformation("{Prop} " + a, [|2|]);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogTrace_InvalidParameterType_StringConcat_NonConstantDisabled()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
var a = "test";
logger.LogInformation("{Prop} " + a, 2);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
""")
              .AddAnalyzerConfiguration("MA0124.allow_non_constant_formats", "false")
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogTrace_InvalidParameterType_NullableGuid()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", [|2|]);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Guid;T:System.Nullable{System.Guid}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogTrace_ValidParameterType_NullableGuid()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", System.Guid.Empty);
logger.LogInformation("{Prop}", (System.Guid?)null);

System.Guid? value1 = null;
System.Guid? value2 = System.Guid.Empty;
logger.LogInformation("{Prop}", value1);
logger.LogInformation("{Prop}", value2);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Guid;System.Nullable{System.Guid}
""")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Logger_LogTrace_ValidParameterType_StringArray()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", new string[1]);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String[]
""")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Logger_LogTrace_ValidParameterType_ValueTuple()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", ("", 1));
logger.LogInformation("{Prop}", (A: "", B: 1));
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.ValueTuple{System.String,System.Int32}
""")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Logger_LogTrace_ValidParameterType_NullableReferenceType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", "");
logger.LogInformation("{Prop}", (string?)null);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.String
""")
              .ValidateAsync();
    }
    
    [Fact]
    public async Task Logger_LogTrace_ValidParameterType_NullableInt32AllowsInt32()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", 1);
logger.LogInformation("{Prop}", (int?)1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Nullable{System.Int32}
""")
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_Int32DoesNotAllowNullableInt32()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", 1);
logger.LogInformation("{Prop}", [||](int?)1);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
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

    [Fact]
    public async Task MissingConfiguration()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation([|"{Prop}"|], 2);
logger.LogInformation("{Dummy}", 2);
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Dummy;System.Int32
""")
              .ShouldReportDiagnosticWithMessage("Log parameter 'Prop' has no configured type")
              .ValidateAsync();
    }

    [Fact]
    public async Task ConfigurationFromAttribute()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

[assembly: Meziantou.Analyzer.Annotations.StructuredLogFieldAttribute("Prop", typeof(string), typeof(long))]

ILogger logger = null;
logger.LogInformation("{Prop}", [|2|]);
logger.LogInformation("{Prop}", 2L);
logger.LogInformation("{Prop}", "");

namespace Meziantou.Analyzer.Annotations
{
    [System.Diagnostics.ConditionalAttribute("MEZIANTOU_ANALYZER_ATTRIBUTES")]
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    internal sealed class StructuredLogFieldAttribute : System.Attribute
    {
        public StructuredLogFieldAttribute(string parameterName, params System.Type[] allowedTypes) { }
    }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAdditionalFile("LoggerParameterTypes.txt", """
Prop;System.Int32
""")
              .ValidateAsync();
    }
}
