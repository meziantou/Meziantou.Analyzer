using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.AvoidUsingRedundantElseAnalyzer,
    Meziantou.Analyzer.Rules.AvoidUsingRedundantElseFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUsingRedundantElseAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    // The following tests aim to validate several combinations affecting
    //  1. whether the AvoidUsingRedundantElse rule is deemed infringed, and
    //  2. the way the code is subsequently fixed.
    //
    // Test code has the form
    //      while
    //          if
    //              jump
    //          else
    //
    // Some of the varying factors are:
    //
    // - Are there
    //      'while' braces?             => If not, we need to add some in the fixed code.
    //      'if' braces?
    //      'else' braces?              => If so, we need to remove them in the fixed code.
    // - Does the 'if' block contain
    //      nested blocks?
    //      local functions?
    // - Is the code misformatted?      => If so, only modified lines should be formatted.

    [Theory]
    [InlineData("break", true)]
    [InlineData("continue", true)]
    [InlineData("goto LABEL", true)]
    [InlineData("return", true)]
    [InlineData("throw new System.ArgumentNullException(nameof(value))", true)]
    [InlineData("value++", false)]
    [InlineData("if (value < -5) return", false)]
    public Task Test_WhenIfJumpsUnconditionally_ElseRemoved(string statement, bool expectElseRemoval)
    {
        var @else = expectElseRemoval ? "{|MA0071:else|}" : "else";
        var originalCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {{statement}};
                        }
                        {{@else}}
                            value--;
                    }
                LABEL:
                    value++;
                }
            }
            """;
        var modifiedCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {{statement}};
                        }

                        value--;
                    }
                LABEL:
                    value++;
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = expectElseRemoval ? modifiedCode : originalCode;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("yield break", true)]
    [InlineData("yield return value", false)]
    [InlineData("if (value < -5) yield break", false)]
    public Task Test_WhenIfYieldJumpsUnconditionally_ElseRemoved(string statement, bool expectElseRemoval)
    {
        var @else = expectElseRemoval ? "{|MA0071:else|}" : "else";
        var originalCode = $$"""
            class TestClass
            {
                System.Collections.Generic.IEnumerable<int> Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            value++;
                            {{statement}};
                        }
                        {{@else}}
                        {
                            value--;
                        }
                    }
                }
            }
            """;
        var modifiedCode = $$"""
            class TestClass
            {
                System.Collections.Generic.IEnumerable<int> Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            value++;
                            {{statement}};
                        }

                        value--;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = expectElseRemoval ? modifiedCode : originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksAndContainsLocalFunction_ElseRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            Increment(ref value);
                            break;
                            void Increment(ref int val) => val++;
                        }
                        {|MA0071:else|}
                        {
                            Decrement(ref value);
                            void Decrement(ref int val)
                            {
                                val--;
                            }
                        }
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            Increment(ref value);
                            break;
                            void Increment(ref int val) => val++;
                        }

                        Decrement(ref value);
                        void Decrement(ref int val)
                        {
                            val--;
                        }
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksFromNestedBlock_ElseRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {
                                break;
                            }
                        }
                        {|MA0071:else|}
                            // Decrement
                            value--;
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {
                                break;
                            }
                        }

                        // Decrement
                        value--;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksFromNestedBlockAndContainsLocalFunction_ElseRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {
                                Increment(ref value);
                                break;
                            }
                            void Increment(ref int val) => val++;
                        }
                        {|MA0071:else|}
                        {
                            {
                                Decrement(ref value);
                            }

                            void Decrement(ref int val)
                            {
                                val--;
                            }
                        }
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            {
                                Increment(ref value);
                                break;
                            }
                            void Increment(ref int val) => val++;
                        }

                        {
                            Decrement(ref value);
                        }

                        void Decrement(ref int val)
                        {
                            val--;
                        }
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksAndWhileWithoutBraces_ElseRemovedAndWhileBracesAdded()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                        if (value < 0)
                        {
                            break;
                        }
                        {|MA0071:else|}
                        {
                            value--;
                        }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                        {
                            break;
                        }

                        value--;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksAndCodeMisformatted_ElseRemovedButOnlyItsStatementsAreFormatted()
    {
        var originalCode = """
            class TestClass
            {
             void Test(){
             var value = -1;
               while (true)
             {if (value < 0)
            {    break;
            }{|MA0071:else|}{                         value--;
             }
            }
            }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
             void Test(){
             var value = -1;
               while (true)
             {if (value < 0)
            {    break;
            }

                        value--;
                    }
            }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksWithEmptyElseBlock_ElseRemovedAndNoEmptyLineAfterIf()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    if (true)
                    {
                        return;
                    }
                    {|MA0071:else|}
                    {
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    if (true)
                    {
                        return;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatBreaksButNoElse_NoDiagnosticReported()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value < 0)
                            break;

                        value++;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ElseIfChainWithReachablePreviousThenAndMethodInvocationCondition_NoDiagnosticReported()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var test = new TestClass();
                    foreach (var cmp in new[] { "first", "second", "third" })
                    {
                        if (test.Verify("first", cmp) is not null)
                        {
                            System.Console.WriteLine("Handled");
                        }
                        else if (test.Verify("second", cmp) is not null)
                        {
                            System.Console.WriteLine("Handled");
                            continue;
                        }
                        else
                        {
                            System.Console.WriteLine("Not handled");
                        }
                    }
                }

                int? Verify(string tag, string cmp)
                {
                    return tag.Equals(cmp, System.StringComparison.Ordinal) ? 1 : null;
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_SeveralNestedIfElseBlocksWithIfsThatJump_AllProblematicElsesRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value > 0)
                        {
                            return;
                        }
                        {|MA0071:else|} if (value < -10)
                        {
                            continue;
                        }
                        {|MA0071:else|}
                        {
                            if (value < 0)
                            {
                                break;
                            }
                            {|MA0071:else|}
                            {
                                value++;
                            }
                        }
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    while (true)
                    {
                        if (value > 0)
                        {
                            return;
                        }

                        if (value < -10)
                        {
                            continue;
                        }

                        if (value < 0)
                        {
                            break;
                        }

                        value++;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("var local = string.Empty;")]
    [InlineData("if (value is string local) {}")]
    [InlineData("int local() => throw null;")]
    [InlineData("switch (value) { case string local: break; }")]
    public Task Test_IfThatReturnsButIfAndElseContainConflictingLocalDeclarations_NoDiagnosticReported(string localDeclaration)
    {
        var originalCode = $$"""
            class TestClass
            {
                void Test()
                {
                    object value = string.Empty;
                    if (value != null)
                    {
                        {{localDeclaration}}
                        return;
                    }
                    else
                    {
                        int local() => throw null;
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatReturnsAndElseContainsUsingStatementLocalDeclaration_NoDiagnosticReported()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    if (value < 0)
                    {
                        return;
                    }
                    else
                    {
                        using var charEnumerator = string.Empty.GetEnumerator();
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatReturnsAndElseContainsUsingStatementSyntax_ElseRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    if (value < 0)
                    {
                        return;
                    }
                    {|MA0071:else|}
                    {
                        using (var charEnumerator = string.Empty.GetEnumerator())
                        {
                        }
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    if (value < 0)
                    {
                        return;
                    }

                    using (var charEnumerator = string.Empty.GetEnumerator())
                    {
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_IfThatReturnsAndElseContainsNestedUsingStatementLocalDeclaration_ElseRemoved()
    {
        var originalCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    if (value < 0)
                    {
                        return;
                    }
                    {|MA0071:else|}
                    {
                        {
                            using var charEnumerator = string.Empty.GetEnumerator();
                        }
                    }
                }
            }
            """;
        var modifiedCode = """
            class TestClass
            {
                void Test()
                {
                    var value = -1;
                    if (value < 0)
                    {
                        return;
                    }

                    {
                        using var charEnumerator = string.Empty.GetEnumerator();
                    }
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;
        test.FixedCode = modifiedCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_EmptyIf()
    {
        var originalCode = """
            using System;
            class TestClass
            {
            void Test()
            {
                try
                {
                    //DoSomething();
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException)
                    {
                        // test
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }

    [Fact]
    public Task Test_ElseIfChainWithBranchThatDoesNotJump_FollowingElsesNotReported()
    {
        var originalCode = """
            class TestClass
            {
                int Test(int value)
                {
                    if (value == 0)
                    {
                        return 0;
                    }
                    {|MA0071:else|} if (value == 1)
                    {
                        value++;
                    }
                    else if (value == 2)
                    {
                        return 2;
                    }
                    else
                    {
                        return -1;
                    }

                    return value;
                }
            }
            """;

        var test = CreateTest();
        test.TestCode = originalCode;

        return test.RunAsync();
    }
}
