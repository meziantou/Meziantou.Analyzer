using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotLogClassifiedDataAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotLogClassifiedDataAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages([
            new PackageIdentity("Microsoft.Extensions.Logging.Abstractions", "8.0.0"),
            new PackageIdentity("Microsoft.Extensions.Compliance.Abstractions", "8.0.0"),
        ]);
        return test;
    }

    [Fact]
    public Task Logger_LogInformation_NoDataClassification()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_Property()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0153:new Dummy().Prop|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_Property_Array()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0153:new Dummy().Prop[0]|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_Field()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0153:new Dummy().Prop|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_Parameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;

            void A([TaxonomyAttribute]int param)
            {
                logger.LogInformation("{Prop}", {|MA0153:param|});
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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_Parameter_AttributeOnType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;

            void A([TaxonomyAttribute]int param)
            {
                logger.LogInformation("{Prop}", {|MA0153:param|});
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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_BeginScope_DataClassification_Property()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.BeginScope("{Prop}", {|MA0153:new Dummy().Prop|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_TypeWithClassifiedProperty()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "true");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            PatientInfo p = new();
            logger.LogInformation("{Patient}", {|MA0153:p|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_TypeWithClassifiedField()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "true");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            PatientInfo p = new();
            logger.LogInformation("{Patient}", {|MA0153:p|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_NoDataClassification_TypeWithNoClassifiedMembers()
    {
        var test = CreateTest();
        test.TestCode = """
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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_ObjectCreationWithClassifiedProperty()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "true");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Patient}", {|MA0153:new PatientInfo()|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_NoDataClassification_PrimitiveType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            int value = 42;
            logger.LogInformation("{Value}", value);

            class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
            {
                public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_NoDataClassification_StringType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            string value = "test";
            logger.LogInformation("{Value}", value);

            class PiiData : Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute
            {
                public PiiData() : base(Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_TypeWithClassifiedProperty_ConfigDisabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "false");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            PatientInfo p = new();
            logger.LogInformation("{Patient}", p);

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_TypeWithClassifiedProperty_ConfigEnabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "true");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            PatientInfo p = new();
            logger.LogInformation("{Patient}", {|MA0153:p|});

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

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_DataClassification_DirectProperty_ConfigDisabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0153.report_types_with_data_classification_attributes", "false");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0153:new Dummy().Prop|});

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

        return test.RunAsync();
    }
}
