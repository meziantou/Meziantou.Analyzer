using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.DoNotRemoveOriginalExceptionFromThrowStatementAnalyzer,
    Meziantou.Analyzer.Rules.DoNotRemoveOriginalExceptionFromThrowStatementFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotRemoveOriginalExceptionFromThrowStatementAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    throw new System.Exception();

                    try
                    {
                        throw new System.Exception();
                    }
                    catch (System.Exception ex)
                    {
                        throw new System.Exception("test", ex);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportDiagnostic_DerivedException()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        {|MA0027:throw ex;|}
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.InvalidOperationException ex)
                    {
                        throw;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        _ = ex;
                        {|MA0027:throw ex;|}
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        _ = ex;
                        throw;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_InLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal System.Action Sample()
                {
                    try
                    {
                        return null;
                    }
                    catch (System.Exception ex)
                    {
                        return () => { throw ex; };
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_InLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        Rethrow();

                        void Rethrow()
                        {
                            throw ex;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NoDiagnostic_InFinallyNestedInCatch()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                        }
                        finally
                        {
                            throw ex;
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ShouldReportDiagnostic_InTryNestedInCatch()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            {|MA0027:throw ex;|}
                        }
                        finally
                        {
                        }
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                internal void Sample()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            throw;
                        }
                        finally
                        {
                        }
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}
