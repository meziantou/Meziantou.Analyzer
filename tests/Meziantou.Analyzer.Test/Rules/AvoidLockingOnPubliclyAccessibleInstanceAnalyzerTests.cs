using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.AvoidLockingOnPubliclyAccessibleInstanceAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidLockingOnPubliclyAccessibleInstanceAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task LockThis_Internal()
    {
        var test = CreateTest();
        test.TestCode = """
            internal class Test
            {
                void A()
                {
                    lock (this) {}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockThis_Public()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                void A()
                {
                    lock ({|MA0064:this|}) {}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockTypeof()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    lock ({|MA0064:typeof(Test)|})
                    {
                        throw null;
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockVariableOfTypeSystemType()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    System.Type type = null;
                    lock ({|MA0064:type|}) {}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockPubliclyAccessibleField()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                public string TestField;
                void A()
                {
                    lock ({|MA0064:TestField|}) {}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockPrivateFieldShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                private string TestField;
                void A()
                {
                    lock (TestField) {}
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LockVariableOfTypeStringShouldNotReport()
    {
        var test = CreateTest();
        test.TestCode = """
            public class Test
            {
                private string TestField;
                void A()
                {
                    string test = "";
                    lock (test) {}
                }
            }
            """;

        return test.RunAsync();
    }
}
