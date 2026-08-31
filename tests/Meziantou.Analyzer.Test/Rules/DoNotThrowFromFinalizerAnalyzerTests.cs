using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotThrowFromFinalizerAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotThrowFromFinalizerAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task Finalizer_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                ~TestClass()
                {
                    {|MA0086:throw new System.Exception("Unbecoming exception");|}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinalizerDoesNotThrow_NoDiagnosticReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                ~TestClass()
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
    public Task FinalizerThrowsFromNestedBlock_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                ~TestClass()
                {
                    var value = 1;
                    try
                    {
                    }
                    finally
                    {
                        {
                            Increment(ref value);
                            {|MA0086:throw new System.Exception($"Unbecoming exception No {value}");|}
                        }
                        void Increment(ref int val) => val++;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FinalizerThrowsFromNestedTryCatchBlock_ExceptionIsHandled_DiagnosticIsReported()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                ~TestClass()
                {
                    try
                    {
                    }
                    finally
                    {
                        try
                        {
                            {|MA0086:throw new System.Exception();|}
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
}
