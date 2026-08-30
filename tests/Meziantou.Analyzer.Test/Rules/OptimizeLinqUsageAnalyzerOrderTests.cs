using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerOrderTests
{
    // This class covers MA0159 only, the way the original test filtered the diagnostics to that rule
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest { ReferenceAssemblies = ReferenceAssemblies.Net.Net80 };
        test.DisabledDiagnostics.Add(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy);
        test.DisabledDiagnostics.Add(RuleIdentifiers.DuplicateEnumerable_OrderBy);
        return test;
    }

    private static DiagnosticResult ExpectedUseOrder(string message) =>
        new DiagnosticResult(RuleIdentifiers.OptimizeEnumerable_UseOrder, DiagnosticSeverity.Info)
            .WithLocation(0)
            .WithMessage(message);

    [Fact]
    public Task IEnumerable_Order_net5()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order_LambdaNotValid()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => true);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order_LambdaReferenceAnotherParameter()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test(int a)
                {
                    IEnumerable<string> query = null;
                    query.OrderBy(x => a);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_Order()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.{|#0:OrderBy|}(x => x);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedUseOrder("Use 'Order' instead of 'OrderBy'"));
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.Order();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task IEnumerable_OrderDescending()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.{|#0:OrderByDescending|}(x => x);
                }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedUseOrder("Use 'OrderDescending' instead of 'OrderByDescending'"));
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Linq;
            class Test
            {
                public Test()
                {
                    IEnumerable<string> query = null;
                    query.OrderDescending();
                }
            }
            """;

        return test.RunAsync();
    }
}
