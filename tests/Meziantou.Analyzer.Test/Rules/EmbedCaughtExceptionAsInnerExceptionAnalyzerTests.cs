using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.EmbedCaughtExceptionAsInnerExceptionAnalyzer,
    Meziantou.Analyzer.Rules.EmbedCaughtExceptionAsInnerExceptionFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EmbedCaughtExceptionAsInnerExceptionAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task NotInCaughtException_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    throw new System.Exception("");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InCaughtExceptionWithInnerException_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        throw new System.Exception("", ex);
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InCaughtExceptionWithoutInnerException_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        throw [|new System.Exception("")|];
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InCaughtExceptionWithoutInnerException_NoConstructorWithInnerException_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        throw new CustomException("");
                    }
                }
            }

            class CustomException : System.Exception
            {
                public CustomException(string message)
                {
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task InCaughtExceptionWithoutInnerException_CodeFix()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public void A()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        throw [|new System.Exception("")|];
                    }
                }
            }
            """;
        test.FixedCode = """
            class Test
            {
                public void A()
                {
                    try
                    {
                    }
                    catch (System.Exception ex)
                    {
                        throw new System.Exception("", ex);
                    }
                }
            }
            """;

        return test.RunAsync();
    }
}
