using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.LoggerParameterTypeAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class LoggerParameterTypeAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddLoggingAbstractions();
        return test;
    }

    [Fact]
    public Task BeginScope_InvalidParameterType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.BeginScope("{Prop} {Name}", {|MA0124:1|}, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task BeginScope_InvalidParameterType_XmlCommentId()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.BeginScope({|MA0135:"{Prop} {Name} {Name}"|}, 1, 2, (int?)null);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.LoggerParameterType_MissingConfiguration, DiagnosticSeverity.Warning).WithLocation(4, 19));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(RuleIdentifiers.LoggerParameterType, DiagnosticSeverity.Warning).WithLocation(4, 43));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Count;T:System.Nullable{System.Int32}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LogInformation_InvalidParameterType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop} {Name}", {|MA0124:1|}, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LogInformation_ValidParameterType2()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop} {Name} {Name}", "test", 2, 3L);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            # This is a comment
            Prop;System.String
            Name;System.Int32;System.Int64
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LogInformation_NoConfigurationFile()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop} {Name}", "test", 2, 3L);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LogInformation_EmptyConfigurationFile()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop} {Name} {Name}", "test", 2, 3L);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """

            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessage_Define_InvalidParameterType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            LoggerMessage.Define<{|MA0124:int|}, string>(LogLevel.Information, new EventId(0), "{Prop} {Name}");
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessage_DefineScope_InvalidParameterType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            LoggerMessage.DefineScope<{|MA0124:int|}, string>("{Prop} {Name}");
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_InvalidParameterType_FormattableString()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation($"{{Prop}} {2}", {|MA0124:2|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_InvalidParameterType_StringConcat()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            var a = "test";
            logger.LogInformation("{Prop} " + a, {|MA0124:2|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_InvalidParameterType_StringConcat_NonConstantDisabled()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0124.allow_non_constant_formats", "false");
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            var a = "test";
            logger.LogInformation("{Prop} " + a, 2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_InvalidParameterType_NullableGuid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0124:2|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Guid;T:System.Nullable{System.Guid}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_ValidParameterType_NullableGuid()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", System.Guid.Empty);
            logger.LogInformation("{Prop}", (System.Guid?)null);

            System.Guid? value1 = null;
            System.Guid? value2 = System.Guid.Empty;
            logger.LogInformation("{Prop}", value1);
            logger.LogInformation("{Prop}", value2);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Guid;System.Nullable{System.Guid}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_ValidParameterType_StringArray()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", new string[1]);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String[]
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_ValidParameterType_ValueTuple()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", ("", 1));
            logger.LogInformation("{Prop}", (A: "", B: 1));
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.ValueTuple{System.String,System.Int32}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_ValidParameterType_NullableReferenceType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", "");
            logger.LogInformation("{Prop}", (string?)null);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_InvalidParameterType_NullableReferenceType()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|#0:(int?)null|});
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0124", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("""Log parameter 'Prop' must be of type 'global::System.Nullable<global::System.String>' but is of type 'global::System.Nullable<global::System.Int32>'"""));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Nullable{System.String}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task ErrorMessageDoesNotAddNullableAnnotation()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|#0:(string?)null|});
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0124", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("""Log parameter 'Prop' must be of type 'global::System.Nullable<global::System.String>' but is of type 'global::System.String'"""));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Nullable{System.String}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogTrace_ValidParameterType_NullableInt32AllowsInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", 1);
            logger.LogInformation("{Prop}", (int?)1);
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Nullable{System.Int32}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Logger_LogInformation_Int32DoesNotAllowNullableInt32()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation("{Prop}", 1);
            logger.LogInformation("{Prop}", {|MA0124:(int?)1|});
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Configuration_UnknownParameterType()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0125", DiagnosticSeverity.Warning).WithLocation("LoggerParameterTypes.txt", 1, 1));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;int
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Configuration_CommentIdToMember()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0125", DiagnosticSeverity.Warning).WithLocation("LoggerParameterTypes.txt", 1, 1));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;M:System.Int32.MaxValue
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task Configuration_DuplicateParameterName()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """

            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0126", DiagnosticSeverity.Warning).WithLocation("LoggerParameterTypes.2.txt", 2, 1));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.1.txt", """
            Prop;System.String
            """));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.2.txt", """
            New;System.String
            Prop;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task MissingConfiguration()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation({|#0:"{Prop}"|}, 2);
            logger.LogInformation("{Dummy}", 2);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0135", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Log parameter 'Prop' has no configured type"));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Dummy;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DeniedParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation({|#0:"{Prop}"|}, 2);
            logger.LogInformation("{Dummy}", 2);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0124", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Log parameter 'Prop' is not allowed by configuration"));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Dummy;System.Int32
            Prop;
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task DeniedParameterWithoutSemiColon()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            ILogger logger = null;
            logger.LogInformation({|#0:"{Prop}"|}, 2);
            logger.LogInformation("{Dummy}", 2);
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0124", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Log parameter 'Prop' is not allowed by configuration"));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Dummy;System.Int32
            Prop
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task ConfigurationFromAttribute()
    {
        var test = CreateTest();
        test.TestState.AddMeziantouAnnotations();
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            [assembly: Meziantou.Analyzer.Annotations.StructuredLogFieldAttribute("Prop", typeof(string), typeof(long))]

            ILogger logger = null;
            logger.LogInformation("{Prop}", {|MA0124:2|});
            logger.LogInformation("{Prop}", 2L);
            logger.LogInformation("{Prop}", "");
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_ValidParameterTypes()
    {
        var test = CreateTest();
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop} and {Name}")]
                static partial void LogTestMessage(ILogger logger, string Prop, int Name);
            }

            class Program { static void Main() { } }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_InvalidParameterType()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop} and {Name}")]
                static partial void LogTestMessage(ILogger logger, int {|MA0124:Prop|}, string Name);
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_MultipleInvalidParameterTypes()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop} and {Name}")]
                static partial void LogTestMessage(ILogger logger, int {|MA0124:Prop|}, int {|MA0124:Name|});
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Prop;System.String
            Name;System.String
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_MissingConfiguration()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop} and {Name}")]
                static partial void LogTestMessage(ILogger logger, string {|#0:Prop|}, int Name);
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0135", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Log parameter 'Prop' has no configured type"));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_DeniedParameter()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop} and {Name}")]
                static partial void LogTestMessage(ILogger logger, string {|#0:Prop|}, int Name);
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0124", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("Log parameter 'Prop' is not allowed by configuration"));
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Name;System.Int32
            Prop;
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_SkipILoggerParameter()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Name}")]
                static partial void LogTestMessage(ILogger logger, int Name);
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_WithCallerMemberName()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;
            using System.Runtime.CompilerServices;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message from {Method} with {Name}")]
                static partial void LogTestMessage(ILogger logger, int Name, [CallerMemberName] string Method = "");
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Method;System.String
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_NullableParameterType()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test with {Value}")]
                static partial void LogTestMessage(ILogger logger, int Value);

                [LoggerMessage(10_005, LogLevel.Trace, "Test with {Value}")]
                static partial void LogTestMessage2(ILogger logger, int? Value);
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Value;System.Nullable{System.Int32}
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_NoConfiguration()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message with {Prop}")]
                static partial void LogTestMessage(ILogger logger, string Prop);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_EmptyFormatString()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "")]
                static partial void LogTestMessage(ILogger logger);
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Name;System.Int32
            """));

        return test.RunAsync();
    }

    [Fact]
    public Task LoggerMessageAttribute_NoFormatParameters()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
        test.TestCode = """
            using Microsoft.Extensions.Logging;

            partial class LoggerExtensions
            {
                [LoggerMessage(10_004, LogLevel.Trace, "Test message without parameters")]
                static partial void LogTestMessage(ILogger logger);
            }
            """;
        test.TestState.AdditionalFiles.Add(("LoggerParameterTypes.txt", """
            Name;System.Int32
            """));

        return test.RunAsync();
    }
}
