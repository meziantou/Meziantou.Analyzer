using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseStringCreateInsteadOfFormattableStringAnalyzer,
    Meziantou.Analyzer.Rules.UseStringCreateInsteadOfFormattableStringFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseStringCreateInsteadOfFormattableStringAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Fact]
    public Task Net5_NoDiagnostic()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
            using System;
            class TypeName
            {
                public void Test()
                {
                    FormattableString.Invariant($"");
                    FormattableString.CurrentCulture($"");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableString_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            class TypeName
            {
                public void Test()
                {
                    FormattableString fs = default;
                    FormattableString.Invariant(fs);
                    FormattableString.CurrentCulture(fs);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Charp9_NoDiagnostic()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp9;
        test.TestCode = """
            using System;
            class TypeName
            {
                public void Test()
                {
                    FormattableString.Invariant($"");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableStringInvariant()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TypeName
            {
                public void Test()
                {
                    {|MA0111:FormattableString.Invariant($"")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    string.Create(CultureInfo.InvariantCulture, $"");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FormattableStringCurrentCulture()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TypeName
            {
                public void Test()
                {
                    {|MA0111:FormattableString.CurrentCulture($"")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    string.Create(CultureInfo.CurrentCulture, $"");
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExpressionTree_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;
            using System.Linq.Expressions;

            class TypeName
            {
                public void Test(IQueryable<int> query)
                {
                    Expression<Func<int, string>> invariant = x => FormattableString.Invariant($"{x}");
                    Expression<Func<int, string>> currentCulture = x => FormattableString.CurrentCulture($"{x}");
                    query.Select(x => FormattableString.Invariant($"{x}"));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Lambda_Diagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class TypeName
            {
                public void Test()
                {
                    Func<int, string> func = x => {|MA0111:FormattableString.Invariant($"{x}")|};
                }
            }
            """;
        test.FixedCode = """
            using System;
            using System.Globalization;

            class TypeName
            {
                public void Test()
                {
                    Func<int, string> func = x => string.Create(CultureInfo.InvariantCulture, $"{x}");
                }
            }
            """;

        return test.RunAsync();
    }
}
