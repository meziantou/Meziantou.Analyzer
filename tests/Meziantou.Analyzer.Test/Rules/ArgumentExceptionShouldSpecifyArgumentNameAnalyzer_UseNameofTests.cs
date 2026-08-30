using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.ArgumentExceptionShouldSpecifyArgumentNameAnalyzer,
    Meziantou.Analyzer.Rules.ArgumentExceptionShouldSpecifyArgumentNameFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer_UseNameofTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Property()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                string Prop
                {
                    get { throw null; }
                    set { throw new System.ArgumentNullException({|MA0043:"value"|}); }
                }
            }
            """;
        test.FixedCode = """
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
    public Task Method()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                string M(string arg0)
                {
                    throw new System.ArgumentNullException({|MA0043:"arg0"|});
                }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                string M(string arg0)
                {
                    throw new System.ArgumentNullException(nameof(arg0));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Operator()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException({|MA0043:"first"|});
                }
            }
            """;
        test.FixedCode = """
            class Sample
            {
                public static Sample operator +(Sample first, Sample second)
                {
                    throw new System.ArgumentNullException(nameof(first));
                }
            }
            """;

        return test.RunAsync();
    }
}
