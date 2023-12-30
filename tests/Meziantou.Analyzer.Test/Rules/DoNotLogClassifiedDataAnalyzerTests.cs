﻿using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
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
    public async Task Logger_BeginScope_DataClassification_Parameter()
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
}
