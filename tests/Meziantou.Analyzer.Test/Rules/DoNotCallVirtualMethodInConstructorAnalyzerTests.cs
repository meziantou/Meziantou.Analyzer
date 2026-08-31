using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotCallVirtualMethodInConstructorAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotCallVirtualMethodInConstructorAnalyzerTests
{
    private static AnalyzerTest CreateTest() => new();

    [Fact]
    public Task CtorWithVirtualCall()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    {|MA0056:A()|};
                }

                public virtual void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithAbstractCall()
    {
        var test = CreateTest();
        test.TestCode = """
            abstract class Test
            {
                Test()
                {
                    {|MA0056:A()|};
                }

                public abstract void A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithNoVirtualCall()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    A();
                }

                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualCallOnAnotherInstance()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    var test = new Test();
                    test.A();
                }

                public virtual void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualPropertyAssignment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    {|MA0056:A|} = 10;
                }

                public virtual int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualPropertyAssignmentOnAnotherInstance()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    var test = new Test();
                    test.A = 10;
                }

                public virtual int A { get; set; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualPropertyGet()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    _ = {|MA0056:A|};
                }

                public virtual int A => 10;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithOverridedMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            class Base
            {
                public virtual void A() { }
            }

            class Test : Base
            {
                Test()
                {
                    {|MA0056:A()|};
                }

                public override void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualPropertyReferenceInNameOf()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    _ = nameof(A);
                }

                public virtual int A => 10;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssignVirtualEvent()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                protected virtual event System.Action SampleEvent;

                Test()
                {
                    {|MA0056:SampleEvent += A|};
                }

                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssignEvent()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                protected event System.Action SampleEvent;

                Test()
                {
                    SampleEvent += A;
                }

                public void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VirtualDelegate()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    System.Action a = A;
                }

                public virtual void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task VirtualDelegate2()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    System.Action a = () => A();
                }

                public virtual void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CtorWithVirtualGetOnlyPropertyAssignment()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                Test()
                {
                    A = 10;
                }

                public virtual int A { get; }
            }
            """;

        return test.RunAsync();
    }
}
