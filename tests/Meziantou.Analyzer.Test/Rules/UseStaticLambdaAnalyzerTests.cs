using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStaticLambdaAnalyzer,
    Meziantou.Analyzer.Rules.UseStaticLambdaFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStaticLambdaAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task LambdaWithoutCapture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list) => list.Sort([|(x, y) => x.CompareTo(y)|]);
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list) => list.Sort(static (x, y) => x.CompareTo(y));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task SimpleLambdaWithoutCapture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, int>)([|x => x + 1|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, int>)(static x => x + 1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AnonymousMethodWithoutCapture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int>)([|delegate { return 1; }|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int>)(static delegate { return 1; });
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AsyncLambdaWithoutCapture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                void M() => _ = (Func<Task<int>>)([|async () => await Task.FromResult(1)|]);
            }
            """;
        test.FixedCode = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                void M() => _ = (Func<Task<int>>)(static async () => await Task.FromResult(1));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultilineLambdaKeepsIndentation()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list)
                {
                    list.Sort(
                        [|(x, y) => x.CompareTo(y)|]);
                }
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list)
                {
                    list.Sort(
                        static (x, y) => x.CompareTo(y));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaInExpressionTree()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq.Expressions;
            class C
            {
                void M() => _ = (Expression<Func<int, int>>)([|x => x + 1|]);
            }
            """;
        test.FixedCode = """
            using System;
            using System.Linq.Expressions;
            class C
            {
                void M() => _ = (Expression<Func<int, int>>)(static x => x + 1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaUsingStaticMemberAndConstant()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                const int Constant = 1;
                static int Field;
                void M() => _ = (Func<int>)([|() => Field + Constant + Compute()|]);
                static int Compute() => 0;
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                const int Constant = 1;
                static int Field;
                void M() => _ = (Func<int>)(static () => Field + Constant + Compute());
                static int Compute() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaUsingNameofOfLocal()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M(int parameter) => _ = (Func<string>)([|() => nameof(parameter)|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M(int parameter) => _ = (Func<string>)(static () => nameof(parameter));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLambdaCapturingLocalOfTheOuterLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, Func<int>>)([|x => { var y = x; return () => y; }|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, Func<int>>)(static x => { var y = x; return () => y; });
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaUsingStaticLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M()
                {
                    static int Local() => 1;
                    _ = (Func<int>)([|() => Local()|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M()
                {
                    static int Local() => 1;
                    _ = (Func<int>)(static () => Local());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaDeclaringItsOwnLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int>)([|() => { int Local() => 1; return Local(); }|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int>)(static () => { int Local() => 1; return Local(); });
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AlreadyStatic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list) => list.Sort(static (x, y) => x.CompareTo(y));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CaptureLocalVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M(int value) => _ = (Func<int>)(() => value);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CaptureInstanceField()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                int _field;
                void M() => _ = (Func<int>)(() => _field);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CaptureThisUsingInstanceMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                int Compute() => 1;
                void M() => _ = (Func<int>)(() => Compute());
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CapturePrimaryConstructorParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C(int value)
            {
                void M() => _ = (Func<int>)(() => value);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLambdaCapturingLocalOfTheEnclosingMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M(int value) => _ = (Func<int, Func<int>>)(x => () => value);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReferenceNonStaticLocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M()
                {
                    int Local() => 1;
                    _ = (Func<int>)(() => Local());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ReferenceNonStaticLocalFunctionAsMethodGroup()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M()
                {
                    int Local() => 1;
                    _ = (Func<Func<int>>)(() => Local);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task QueryExpression()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> items) => _ = from item in items where item > 0 select item;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaCapturingRangeVariable()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> items) => _ = from item in items select (Func<int>)(() => item);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CSharp8()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp8;
        test.TestCode = """
            using System.Collections.Generic;
            class C
            {
                void M(List<int> list) => list.Sort((x, y) => x.CompareTo(y));
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LambdaWithAttribute()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, int>)([|[Obsolete] (int x) => x|]);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M() => _ = (Func<int, int>)([Obsolete] static (int x) => x);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task MultipleLambdasWithoutCapture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M()
                {
                    _ = (Func<int>)([|() => 1|]);
                    _ = (Func<int>)([|() => 2|]);
                }
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M()
                {
                    _ = (Func<int>)(static () => 1);
                    _ = (Func<int>)(static () => 2);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NestedLambdaInsideCapturingLambda()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                void M(int value) => _ = (Func<Func<int>>)(() => value > 0 ? [|() => 1|] : null);
            }
            """;
        test.FixedCode = """
            using System;
            class C
            {
                void M(int value) => _ = (Func<Func<int>>)(() => value > 0 ? static () => 1 : null);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TopLevelStatements()
    {
        var test = CreateTest();
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        test.TestCode = """
            using System;
            var value = 1;
            _ = (Func<int>)([|() => 1|]);
            _ = (Func<int>)(() => value);
            """;
        test.FixedCode = """
            using System;
            var value = 1;
            _ = (Func<int>)(static () => 1);
            _ = (Func<int>)(() => value);
            """;

        return test.RunAsync();
    }

#if CSHARP14_OR_GREATER
    [Fact]
    public Task CaptureThisUsingFieldKeyword()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class C
            {
                public int P
                {
                    get
                    {
                        Func<int> f = () => field;
                        return f();
                    }
                }
            }
            """;

        return test.RunAsync();
    }
#endif
}
