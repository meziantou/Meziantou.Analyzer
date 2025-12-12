using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

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
    public async Task Logger_LogInformation_DataClassification_TypeWithClassifiedProperty()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
PatientInfo p = new();
logger.LogInformation("{Patient}", [|p|]);

class PatientInfo
{
    [PiiData] public string PatientId { get; set; }
    public ulong RecordId { get; set; }
    [PiiData] public string FirstName { get; set; }
}

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_TypeWithClassifiedField()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
PatientInfo p = new();
logger.LogInformation("{Patient}", [|p|]);

class PatientInfo
{
    [PiiData] public string PatientId;
    public ulong RecordId;
}

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_NoDataClassification_TypeWithNoClassifiedMembers()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
PatientInfo p = new();
logger.LogInformation("{Patient}", p);

class PatientInfo
{
    public string PatientId { get; set; }
    public ulong RecordId { get; set; }
}

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_DataClassification_ObjectCreationWithClassifiedProperty()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
logger.LogInformation("{Patient}", [|new PatientInfo()|]);

class PatientInfo
{
    [PiiData] public string PatientId { get; set; }
    public ulong RecordId { get; set; }
}

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_NoDataClassification_PrimitiveType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
int value = 42;
logger.LogInformation("{Value}", value);

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Logger_LogInformation_NoDataClassification_StringType()
    {
        const string SourceCode = """
using Microsoft.Extensions.Logging;

ILogger logger = null;
string value = "test";
logger.LogInformation("{Value}", value);

class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
{
    public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
