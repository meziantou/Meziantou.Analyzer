namespace Meziantou.Analyzer.Test.Rules;

public sealed class AvoidUsingRedundantElseAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<AvoidUsingRedundantElseAnalyzer>()
            .WithCodeFixProvider<AvoidUsingRedundantElseFixer>();
    }

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
    public async Task Test_WhenIfJumpsUnconditionally_ElseRemoved(string statement, bool expectElseRemoval)
    {
        var @else = expectElseRemoval ? "[|else|]" : "else";
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(expectElseRemoval ? modifiedCode : originalCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("yield break", true)]
    [InlineData("yield return value", false)]
    [InlineData("if (value < -5) yield break", false)]
    public async Task Test_WhenIfYieldJumpsUnconditionally_ElseRemoved(string statement, bool expectElseRemoval)
    {
        var @else = expectElseRemoval ? "[|else|]" : "else";
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(expectElseRemoval ? modifiedCode : originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksAndContainsLocalFunction_ElseRemoved()
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
                        [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksFromNestedBlock_ElseRemoved()
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
                        [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksFromNestedBlockAndContainsLocalFunction_ElseRemoved()
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
                        [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksAndWhileWithoutBraces_ElseRemovedAndWhileBracesAdded()
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
                        [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksAndCodeMisformatted_ElseRemovedButOnlyItsStatementsAreFormatted()
    {
        var originalCode = """
            class TestClass
            {
             void Test(){
             var value = -1;
               while (true)
             {if (value < 0)
            {    break;
            }[|else|]{                         value--;
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksWithEmptyElseBlock_ElseRemovedAndNoEmptyLineAfterIf()
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
                    [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatBreaksButNoElse_NoDiagnosticReported()
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_ElseIfChainWithReachablePreviousThenAndMethodInvocationCondition_NoDiagnosticReported()
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_SeveralNestedIfElseBlocksWithIfsThatJump_AllProblematicElsesRemoved()
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
                        [|else|] if (value < -10)
                        {
                            continue;
                        }
                        [|else|]
                        {
                            if (value < 0)
                            {
                                break;
                            }
                            [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldBatchFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("var local = string.Empty;")]
    [InlineData("if (value is string local) {}")]
    [InlineData("int local() => throw null;")]
    [InlineData("switch (value) { case string local: break; }")]
    public async Task Test_IfThatReturnsButIfAndElseContainConflictingLocalDeclarations_NoDiagnosticReported(string localDeclaration)
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatReturnsAndElseContainsUsingStatementLocalDeclaration_NoDiagnosticReported()
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatReturnsAndElseContainsUsingStatementSyntax_ElseRemoved()
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
                    [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_IfThatReturnsAndElseContainsNestedUsingStatementLocalDeclaration_ElseRemoved()
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
                    [|else|]
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_EmptyIf()
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
        await CreateProjectBuilder()
                .WithSourceCode(originalCode)
                .ValidateAsync();
    }

    [Fact]
    public async Task Test_ElseIfChainWithBranchThatDoesNotJump_FollowingElsesNotReported()
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
                    [|else|] if (value == 1)
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    // The chain used to be analyzed once per else clause, each time re-analyzing every branch above it,
    // which is quadratic: 1600 branches took more than 4 minutes. Keep the chain long enough that a
    // reintroduction of that behavior is obvious in the duration of this test, but short enough for the
    // matching of the expected and the actual diagnostics of the Roslyn test framework, which recurses
    // once per diagnostic and overflows the stack around a thousand of them.
    [Fact]
    public async Task Test_LongElseIfChainWhereEveryBranchJumps_AllElsesReported()
    {
        const int BranchCount = 500;

        var sourceCode = new StringBuilder();
        sourceCode.AppendLine("""
            class TestClass
            {
                int Test(int value)
                {
                    if (value == 0)
                    {
                        return 0;
                    }
            """);

        for (var i = 1; i < BranchCount; i++)
        {
            var branch = i.ToString(CultureInfo.InvariantCulture);
            sourceCode.AppendLine("        [|else|] if (value == " + branch + ")");
            sourceCode.AppendLine("        {");
            sourceCode.AppendLine("            return " + branch + ";");
            sourceCode.AppendLine("        }");
        }

        sourceCode.AppendLine("""
                    [|else|]
                    {
                        return -1;
                    }
                }
            }
            """);

        // Only the analyzer is exercised: an "else if" chain is nested syntax, so a chain this long is a
        // 1000-level deep tree, and the formatter Roslyn runs when a code action computes its changes
        // recurses once per level and throws InsufficientExecutionStackException
        await new ProjectBuilder()
              .WithAnalyzer<AvoidUsingRedundantElseAnalyzer>()
              .WithSourceCode(sourceCode.ToString())
              .ValidateAsync();
    }
}
