using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.ObsoleteAttributesShouldIncludeExplanationsAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ObsoleteAttributesShouldIncludeExplanationsAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task HasMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [System.Obsolete("message")]
                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete()|}]
                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Class_HasMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [System.Obsolete("message")]
            class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Class_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            class Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            interface ITest
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Struct_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            struct Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Delegate_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            delegate void Test();
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Enum_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            enum Test
            {
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EnumMember_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            enum Test
            {
                [{|MA0070:System.Obsolete|}]
                A,
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Constructor_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public Test() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_HasMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [System.Obsolete("message")]
                public int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Property_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PropertyAccessor_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public int A
                {
                    [{|MA0070:System.Obsolete|}]
                    get => 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Indexer_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public int this[int index] => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_HasMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [System.Obsolete("message")]
                public int A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public int A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_MultipleDeclarators_HasNoMessage_ReportsOnce()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public int A, B;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Event_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public event System.EventHandler A;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Event_MultipleDeclarators_HasNoMessage_ReportsOnce()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete|}]
                public event System.EventHandler A, B;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Record_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            [{|MA0070:System.Obsolete|}]
            record Test(int A);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RecordProperty_HasNoMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            record Test([property: {|MA0070:System.Obsolete|}] int A);
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HasOnlyNamedArgument()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                [{|MA0070:System.Obsolete(DiagnosticId = "ID1")|}]
                public void A() { }
            }
            """;

        return test.RunAsync();
    }
}
