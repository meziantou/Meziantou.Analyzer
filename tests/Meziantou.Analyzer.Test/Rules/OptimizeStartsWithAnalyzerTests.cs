using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeStartsWithAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeStartsWithFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeStartsWithAnalyzerTests
{
    private static CodeFixTest CreateTest() => new() { ReferenceAssemblies = ReferenceAssemblies.Net.Net70 };

    [Theory]
    [InlineData("null")]
    [InlineData(@"""""")]
    [InlineData(@"str")]
    [InlineData(@"""abc""")]
    [InlineData(@"""abc"", ignoreCase: true, null")]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", StringComparison.CurrentCultureIgnoreCase")]
    [InlineData(@"""a"", StringComparison.InvariantCultureIgnoreCase")]
    [InlineData(@"""a""")]
    [InlineData(@"""a"", StringComparison.CurrentCulture")]
    [InlineData(@"""a"", StringComparison.InvariantCulture")]
    public Task StartsWith_NoReport(string method)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.StartsWith({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("""[|"a"|], StringComparison.Ordinal""", """'a'""")]
    public Task StartsWith_Report(string method, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.StartsWith({{method}});
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.StartsWith({{fix}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData(@"""""")]
    [InlineData(@"str")]
    [InlineData(@"""abc""")]
    [InlineData(@"""abc"", ignoreCase: true, null")]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", StringComparison.CurrentCultureIgnoreCase")]
    [InlineData(@"""a"", StringComparison.InvariantCultureIgnoreCase")]
    [InlineData(@"""a""")]
    [InlineData(@"""a"", StringComparison.CurrentCulture")]
    [InlineData(@"""a"", StringComparison.InvariantCulture")]
    public Task EndsWith_NoReport(string method)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.EndsWith({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("""[|"a"|], StringComparison.Ordinal""", """'a'""")]
    public Task EndsWith_Report(string method, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.EndsWith({{method}});
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.EndsWith({{fix}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"[|""a""|], StringComparison.Ordinal", @"'a', StringComparison.Ordinal")]
    [InlineData(@"[|""a""|], StringComparison.CurrentCulture", @"'a', StringComparison.CurrentCulture")]
    [InlineData(@"[|""a""|], 1, 2, StringComparison.Ordinal", @"'a', 1, 2")]
    [InlineData(@"[|""a""|], 1, StringComparison.Ordinal", @"'a', 1")]
    public Task IndexOf_Report(string method, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.IndexOf({{method}});
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.IndexOf({{fix}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData(@"""""")]
    [InlineData(@"str")]
    [InlineData(@"""abc""")]
    [InlineData(@"""a""")]
    [InlineData(@"""a"", 1")]
    [InlineData(@"""a"", 1, 2")]
    [InlineData(@"""a"", 1, 2, StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", 1, StringComparison.OrdinalIgnoreCase")]
    public Task IndexOf_NoReport(string method)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.IndexOf({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    public Task IndexOf_NoReport_Netstandard2_0(string method)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.IndexOf({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"[|""a""|], StringComparison.Ordinal", @"'a'")]
    [InlineData(@"[|""a""|], 1, 2, StringComparison.Ordinal", @"'a', 1, 2")]
    [InlineData(@"[|""a""|], 1, StringComparison.Ordinal", @"'a', 1")]
    public Task LastIndexOf_Report(string method, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.LastIndexOf({{method}});
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.LastIndexOf({{fix}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData(@"""""")]
    [InlineData(@"str")]
    [InlineData(@"""abc""")]
    [InlineData(@"""a""")]
    [InlineData(@"""a"", 1")]
    [InlineData(@"""a"", 1, 2")]
    [InlineData(@"""a"", StringComparison.CurrentCulture")]
    [InlineData(@"""a"", 1, 2, StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", 1, StringComparison.OrdinalIgnoreCase")]
    public Task LastIndexOf_NoReport(string method)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.LastIndexOf({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    public Task LastIndexOf_NoReport_Netstandard2_0(string method)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = $$"""

            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.LastIndexOf({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""ab"", """"")]
    [InlineData(@"""ab"", ""c""")]
    [InlineData(@"""a"", ""bc""")]
    [InlineData(@"""a"", ""b"", StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", ""b"", StringComparison.CurrentCulture")]
    [InlineData(@"""a"", ""b"", false, null")]
    public Task Replace_NoReport(string method)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.Replace({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"""a"", ""b""", @"'a', 'b'")]
    [InlineData(@"""a"", ""b"", StringComparison.Ordinal", @"'a', 'b'")]
    public Task Replace_Report(string method, string fix)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.[|Replace|]({{method}});
                }
            }
            """;
        test.FixedCode = $$"""
            using System;
            class Test
            {
                void A(string str)
                {
                    _ = str.Replace({{fix}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"separator: [|"",""|], new object[0]")]
    [InlineData(@"[|"",""|], new object[0]")]
    [InlineData(@"[|"",""|], new string[0]")]
    [InlineData(@"[|"",""|], new string[0], 0, 1")]
    [InlineData(@"[|"",""|], Enumerable.Empty<object>()")]
    [InlineData(@"[|"",""|], Enumerable.Empty<string>()")]
    public Task Join_Report(string method)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        test.TestCode = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Test
            {
                void A()
                {
                    _ = string.Join({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@""","", new object[0]")]
    [InlineData(@""","", new string[0]")]
    [InlineData(@""","", new string[0], 0, 1")]
    [InlineData(@""","", Enumerable.Empty<object>()")]
    [InlineData(@""","", Enumerable.Empty<string>()")]
    public Task Join_NoReport_netstandard2_0(string method)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Test
            {
                void A()
                {
                    _ = string.Join({{method}});
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData(@"null, new object[0]")]
    [InlineData(@"""ab"", new object[0]")]
    [InlineData(@"""ab"", new string[0]")]
    [InlineData(@"""ab"", new string[0], 0, 1")]
    [InlineData(@"""ab"", Enumerable.Empty<object>()")]
    [InlineData(@"""ab"", Enumerable.Empty<string>()")]
    [InlineData(@"',', new object[0]")]
    [InlineData(@"',', new string[0]")]
    [InlineData(@"',', new string[0], 0, 1")]
    [InlineData(@"',', Enumerable.Empty<object>()")]
    [InlineData(@"',', Enumerable.Empty<string>()")]
    public Task Join_NoReport(string method)
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
        test.TestCode = $$"""

            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Test
            {
                void A()
                {
                    _ = string.Join({{method}});
                }
            }
            """;

        return test.RunAsync();
    }
}
