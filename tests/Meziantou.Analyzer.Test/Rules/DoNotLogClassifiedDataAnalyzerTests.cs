using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class DoNotLogClassifiedDataAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<DoNotLogClassifiedDataAnalyzer>()
            .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest)
            .WithTargetFramework(TargetFramework.Net8_0)
            .AddNuGetReference("Microsoft.Extensions.Logging.Abstractions", "8.0.0", "lib/net8.0")
            .AddNuGetReference("Microsoft.Extensions.Compliance.Abstractions", "8.0.0", "lib/net8.0");

    [Fact]
    public async Task Logger_LogInformation_NoDataClassification()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", new Dummy().Prop);

class Dummy
{
    public string Prop { get; set; }
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_Property()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", [|new Dummy().Prop|]);

class Dummy
{
    [TaxonomyAttribute()]
    public string Prop { get; set; }
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_Property_Array()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", [|new Dummy().Prop[0]|]);

class Dummy
{
    [TaxonomyAttribute()]
    public string[] Prop { get; set; }
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_Field()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Prop}", [|new Dummy().Prop|]);

class Dummy
{
    [TaxonomyAttribute()]
    public string Prop;
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_Parameter()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;

void A([TaxonomyAttribute]int param)
{
    logger.LogInformation("{Prop}", [|param|]);
}

class Dummy
{
    [TaxonomyAttribute()]
    public string Prop;
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_Parameter_AttributeOnType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;

void A([TaxonomyAttribute]int param)
{
    logger.LogInformation("{Prop}", [|param|]);
}

[TaxonomyAttribute()]
class Dummy
{
    public string Prop;
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_BeginScope_DataClassification_Property()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;


ILogger logger = null;
logger.BeginScope("{Prop}", [|new Dummy().Prop|]);

class Dummy
{
    [TaxonomyAttribute()]
    public string Prop;
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_NoDataClassification()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

partial class LoggerExtensions
{
    [LoggerMessage(10_004, LogLevel.Trace, "Test message with parameters {Method} {Threshold42} {Table21}")]
    static partial void LogTestMessage(ILogger logger, string table21, float threshold42, [CallerMemberName] string method = "");
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}

class Program { static void Main() { } }
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_DataClassification_Parameter()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

partial class LoggerExtensions
{
    [LoggerMessage(10_004, LogLevel.Trace, "Test message with parameters {Method} {Threshold42} {Table21}")]
    static partial void LogTestMessage(ILogger logger, [TaxonomyAttribute] string [|table21|], float threshold42, [CallerMemberName] string method = "");
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}

class Program { static void Main() { } }
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_DataClassification_ParameterType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

partial class LoggerExtensions
{
    [LoggerMessage(10_004, LogLevel.Trace, "Test message with parameters {Method} {Threshold42} {Table21}")]
    static partial void LogTestMessage(ILogger logger, ClassifiedData [|table21|], float threshold42, [CallerMemberName] string method = "");
}

[TaxonomyAttribute]
class ClassifiedData
{
    public string Value { get; set; }
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}

class Program { static void Main() { } }
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_DataClassification_MultipleParameters()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

partial class LoggerExtensions
{
    [LoggerMessage(10_004, LogLevel.Trace, "Test message with parameters {Method} {Threshold42} {Table21}")]
    static partial void LogTestMessage(ILogger logger, [TaxonomyAttribute] string [|table21|], [TaxonomyAttribute] float [|threshold42|], [CallerMemberName] string method = "");
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}

class Program { static void Main() { } }
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task LoggerMessage_DataClassification_SkipLoggerParameter()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

partial class LoggerExtensions
{
    [LoggerMessage(10_004, LogLevel.Trace, "Test message")]
    static partial void LogTestMessage(ILogger logger);
}

class TaxonomyAttribute : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public TaxonomyAttribute() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}

class Program { static void Main() { } }
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
