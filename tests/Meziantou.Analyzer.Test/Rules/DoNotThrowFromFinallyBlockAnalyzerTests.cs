using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotThrowFromFinallyBlockAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotThrowFromFinallyBlockAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task FinallyThrowsDirectly_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    try
                    {
                    }
                    finally
                    {
                        [|throw new System.Exception("Unbecoming exception");|]
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyDoesNotThrow_NoDiagnosticReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    var value = 1;
                    try
                    {
                    }
                    finally
                    {
                        value++;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyThrowsFromNestedBlock_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    var value = 1;
                    try
                    {
                    }
                    finally
                    {
                        {
                            Increment(ref value);
                            [|throw new System.Exception($"Unbecoming exception No {value}");|]
                        }
                        void Increment(ref int val) => val++;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyThrowsFromNestedTryCatchBlock_ExceptionIsHandled_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    try
                    {
                    }
                    finally
                    {
                        try
                        {
                            [|throw new System.Exception();|]
                        }
                        catch
                        {
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyThrowsFromNestedTryCatchBlock_ExceptionIsUnhandled_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    try
                    {
                    }
                    finally
                    {
                        try
                        {
                            [|throw new System.Exception();|]
                        }
                        catch (System.ArgumentException)
                        {
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinallyThrowsFromSeveralLocations_DiagnosticIsReportedForEachOne()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    try
                    {
                    }
                    finally
                    {
                        if (true)
                        {
                            [|throw new System.Exception();|]
                        }
                        else
                        {
                            [|throw new System.Exception();|]
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}
