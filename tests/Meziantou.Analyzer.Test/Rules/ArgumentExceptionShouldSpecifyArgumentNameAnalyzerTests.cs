using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.DisabledDiagnostics.Add("MA0043");
        return test;
    }

    [Fact]
    public Task ArgumentNameIsSpecified_Record_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            internal sealed record ManuscriptId(int Id)
            {
                public int Id { get; } = Id > 0 ? Id : throw new ArgumentOutOfRangeException(paramName: nameof(Id), Id, message: "Must be greater than 0");
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_LocalFunction_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void Test(string test)
                {
                    void LocalFunction(string a)
                    {
                        throw new System.ArgumentNullException(nameof(a));
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_LocalFunction_Static_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void Test(string test)
                {
                    static void LocalFunction(string a)
                    {
                        throw new System.ArgumentNullException({|MA0015:"test"|});
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_LocalFunction_ArgumentFromParentMethod_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                void Test(string test)
                {
                    void LocalFunction()
                    {
                        throw new System.ArgumentNullException(nameof(test));
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_Operator_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException(nameof(first));
                    throw new System.ArgumentNullException(nameof(second));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_Method_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                Sample(string test)
                {
                    throw new System.Exception();
                    throw new System.ArgumentException("message", nameof(test));
                    throw new System.ArgumentNullException(nameof(test));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_Indexer_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                string this[int index]
                {
                    get { throw new System.ArgumentNullException(nameof(index)); }
                    set { throw new System.ArgumentNullException(nameof(index)); }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameIsSpecified_Setter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException(nameof(value)); }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameDoesNotMatchAParameter_Properties_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException({|#0:"unknown"|}); }
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("'unknown' is not a valid parameter name"));

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNameDoesNotMatchAParameter_Methods_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    throw new System.ArgumentException("message", {|#0:"unknown"|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("'unknown' is not a valid parameter name"));

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadWithoutParameterName_Properties_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                string Prop
                {
                    get { throw null; }
                    set { throw {|MA0015:new System.ArgumentNullException()|}; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OverloadWithoutParameterName_Methods_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    throw {|MA0015:new System.ArgumentException("message")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidParameterName_Lambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>((int a) =>
                    {
                        throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: "address out of range");
                        throw new System.ArgumentOutOfRangeException(paramName: nameof(test), a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterName_Lambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>((int a) =>
                    {
                        if (a < 0)
                            throw new System.ArgumentOutOfRangeException(paramName: {|MA0015:"dummy"|}, a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterName_StaticLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>(static (int a) =>
                    {
                        if (a < 0)
                            throw new System.ArgumentOutOfRangeException(paramName: {|MA0015:"test"|}, a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidParameterName_LambdaWithoutParentheses()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>(a =>
                    {
                        throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidParameterName_StaticLambdaWithoutParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action(static () =>
                    {
                        throw new System.ArgumentNullException({|MA0015:"test"|});
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterName_Delegate()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>(delegate (int a)
                    {
                        if (a < 0)
                            throw new System.ArgumentOutOfRangeException(paramName: {|MA0015:"dummy"|}, a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ValidParameterName_Delegate()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>(delegate (int a)
                    {
                        if (a < 0)
                            throw new System.ArgumentOutOfRangeException(paramName: nameof(a), a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InvalidParameterName_StaticDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestAttribute
            {
                void Test(string test)
                {
                    _ = new System.Action<int>(static delegate (int a)
                    {
                        throw new System.ArgumentOutOfRangeException(paramName: {|MA0015:"test"|}, a, message: "address out of range");
                    });
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoCtorWithParameterName()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;

            void Sample1(string str)
            {
                throw new CustomArgumentException("Message");
            }

            void Sample2(string str)
            {
                throw new CustomArgumentException("Message", new InvalidOperationException());
            }

            public class CustomArgumentException : ArgumentException
            {
                public CustomArgumentException(string message)
                    : base(message)
                {
                }

                public CustomArgumentException(string message, Exception cause)
                    : base(message, cause)
                {
                }

                public CustomArgumentException(string message, string description, Exception cause)
                    : base(message, cause)
                {
                }
             }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrimaryConstructor()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            using System;

            public class Sample(string id)
            {
                void A() => throw new ArgumentException("", nameof(id));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(test);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull({|MA0015:Name|});
                }

                public static string Name { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNullOrEmpty_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrEmpty(test);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNullOrEmpty_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrEmpty({|MA0015:Name|});
                }

                public static string Name { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNullOrWhiteSpace_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrWhiteSpace(test);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNullOrWhiteSpace_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNullOrWhiteSpace({|MA0015:Name|});
                }

                public static string Name { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrEmpty_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty(test);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrEmpty_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty({|MA0015:Name|});
                }

                public static string Name { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrWhiteSpace_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(test);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrWhiteSpace_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace({|MA0015:Name|});
                }

                public static string Name { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithValidParamNameArgument_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(test, nameof(test));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithInvalidParamNameArgument_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(test, {|#0:"invalid"|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("'invalid' is not a valid parameter name"));

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrEmpty_WithValidParamNameArgument_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty(test, nameof(test));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentException_ThrowIfNullOrEmpty_WithInvalidParamNameArgument_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentException.ThrowIfNullOrEmpty(test, {|#0:"invalid"|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("'invalid' is not a valid parameter name"));

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentOutOfRangeException_ThrowIfNegative_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative(value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentOutOfRangeException_ThrowIfNegative_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfNegative({|MA0015:Count|});
                }

                public static int Count { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentOutOfRangeException_ThrowIfNegativeOrZero_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentOutOfRangeException_ThrowIfGreaterThan_ValidParameter_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentOutOfRangeException_ThrowIfGreaterThanOrEqual_InvalidParameter_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(int value)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual({|MA0015:MaxValue|}, 100);
                }

                public static int MaxValue { get; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithNullExpression_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull({|MA0015:""|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithNullExpressionAndValidParamName_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull("", nameof(test));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithNullExpressionAndInvalidParamName_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull("", {|MA0015:"invalid"|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithBooleanExpression_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull({|#0:0 == 1|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("The expression does not match a parameter"));

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithBooleanExpressionAndValidParamName_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(0 == 1, nameof(test));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_WithBooleanExpressionAndInvalidParamName_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull(0 == 1, {|#0:"invalid"|});
                }
            }
            """;
        test.ExpectedDiagnostics.Add(new DiagnosticResult("MA0015", DiagnosticSeverity.Warning).WithLocation(0).WithMessage("'invalid' is not a valid parameter name"));

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_MemberAccess_OptionDisabled_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    ArgumentNullException.ThrowIfNull({|MA0015:request.Definition|});
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_MemberAccess_OptionEnabled_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0015.consider_member_access_as_parameter", "true");
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    ArgumentNullException.ThrowIfNull(request.Definition);
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_DeepMemberAccess_OptionEnabled_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0015.consider_member_access_as_parameter", "true");
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    ArgumentNullException.ThrowIfNull(request.Inner.Definition);
                }
            }
            class Inner { public string? Definition { get; set; } }
            class Request { public Inner? Inner { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_MemberAccess_NonParameterRoot_OptionEnabled_ShouldReportError()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0015.consider_member_access_as_parameter", "true");
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(string test)
                {
                    ArgumentNullException.ThrowIfNull({|MA0015:Name.Length|});
                }

                public static string Name { get; } = "";
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_ExplicitDottedParamName_OptionEnabled_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0015.consider_member_access_as_parameter", "true");
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    ArgumentNullException.ThrowIfNull(request.Definition, "request.Definition");
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ThrowIfNull_ExplicitDottedParamName_OptionDisabled_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    ArgumentNullException.ThrowIfNull(request.Definition, {|MA0015:"request.Definition"|});
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNullException_Constructor_DottedParamName_OptionEnabled_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0015.consider_member_access_as_parameter", "true");
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    if (request.Definition is null)
                        throw new ArgumentNullException("request.Definition");
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArgumentNullException_Constructor_DottedParamName_OptionDisabled_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class Sample
            {
                void Test(Request request)
                {
                    if (request.Definition is null)
                        throw new ArgumentNullException({|MA0015:"request.Definition"|});
                }
            }
            class Request { public string? Definition { get; set; } }
            """;

        return test.RunAsync();
    }
}
