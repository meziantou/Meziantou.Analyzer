using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeStartsWithAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeStartsWithAnalyzer>()
            .WithCodeFixProvider<OptimizeStartsWithFixer>()
            .WithTargetFramework(TargetFramework.Net7_0);
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
    public async Task StartsWith_NoReport(string method)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.StartsWith({{method}});
                        }
                    }
                    """)
              .ValidateAsync();
    }
    
    [Theory]
    [InlineData("""[|"a"|], StringComparison.Ordinal""", """'a', StringComparison.Ordinal""")]
    public async Task StartsWith_Report(string method, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.StartsWith({{method}});
                        }
                    }
                    """)
              .ShouldFixCodeWith($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.StartsWith({{fix}});
                        }
                    }
                    """)
              .ValidateAsync();
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
    public async Task EndsWith_NoReport(string method)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.EndsWith({{method}});
                        }
                    }
                    """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("""[|"a"|], StringComparison.Ordinal""", """'a', StringComparison.Ordinal""")]
    public async Task EndsWith_Report(string method, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.EndsWith({{method}});
                        }
                    }
                    """)
              .ShouldFixCodeWith($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.EndsWith({{fix}});
                        }
                    }
                    """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"[|""a""|], StringComparison.Ordinal", @"'a', StringComparison.Ordinal")]
    [InlineData(@"[|""a""|], StringComparison.CurrentCulture", @"'a', StringComparison.CurrentCulture")]
    [InlineData(@"[|""a""|], 1, 2, StringComparison.Ordinal", @"'a', 1, 2, StringComparison.Ordinal")]
    [InlineData(@"[|""a""|], 1, StringComparison.Ordinal", @"'a', 1, StringComparison.Ordinal")]
    public async Task IndexOf_Report(string method, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                  using System;
                  class Test
                  {
                      void A(string str)
                      {
                          _ = str.IndexOf({{method}});
                      }
                  }
                  """)
              .ShouldFixCodeWith($$"""
                  using System;
                  class Test
                  {
                      void A(string str)
                      {
                          _ = str.IndexOf({{fix}});
                      }
                  }
                  """)
              .ValidateAsync();
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
    public async Task IndexOf_NoReport(string method)
    {
        var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.IndexOf(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    public async Task IndexOf_NoReport_Netstandard2_0(string method)
    {
        var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.IndexOf(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.NetStandard2_0)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"[|""a""|], StringComparison.Ordinal", @"'a', StringComparison.Ordinal")]
    [InlineData(@"[|""a""|], 1, 2, StringComparison.Ordinal", @"'a', 1, 2, StringComparison.Ordinal")]
    [InlineData(@"[|""a""|], 1, StringComparison.Ordinal", @"'a', 1, StringComparison.Ordinal")]
    public async Task LastIndexOf_Report(string method, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                  using System;
                  class Test
                  {
                      void A(string str)
                      {
                          _ = str.LastIndexOf({{method}});
                      }
                  }
                  """)
              .ShouldFixCodeWith($$"""
                  using System;
                  class Test
                  {
                      void A(string str)
                      {
                          _ = str.LastIndexOf({{fix}});
                      }
                  }
                  """)
              .ValidateAsync();
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
    public async Task LastIndexOf_NoReport(string method)
    {
        var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.LastIndexOf(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""a"", StringComparison.OrdinalIgnoreCase")]
    public async Task LastIndexOf_NoReport_Netstandard2_0(string method)
    {
        var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.LastIndexOf(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.NetStandard2_0)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""ab"", """"")]
    [InlineData(@"""ab"", ""c""")]
    [InlineData(@"""a"", ""bc""")]
    [InlineData(@"""a"", ""b"", StringComparison.OrdinalIgnoreCase")]
    [InlineData(@"""a"", ""b"", StringComparison.CurrentCulture")]
    [InlineData(@"""a"", ""b"", false, null")]
    public async Task Replace_NoReport(string method)
    {
        var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.Replace(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"""a"", ""b""", @"'a', 'b'")]
    [InlineData(@"""a"", ""b"", StringComparison.Ordinal", @"'a', 'b', StringComparison.Ordinal")]
    public async Task Replace_Report(string method, string fix)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = [||]str.Replace({{method}});
                        }
                    }
                    """)
              .ShouldFixCodeWith($$"""
                    using System;
                    class Test
                    {
                        void A(string str)
                        {
                            _ = str.Replace({{fix}});
                        }
                    }
                    """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@"separator: [|"",""|], new object[0]")]
    [InlineData(@"[|"",""|], new object[0]")]
    [InlineData(@"[|"",""|], new string[0]")]
    [InlineData(@"[|"",""|], new string[0], 0, 1")]
    [InlineData(@"[|"",""|], Enumerable.Empty<object>()")]
    [InlineData(@"[|"",""|], Enumerable.Empty<string>()")]
    public async Task Join_Report(string method)
    {
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test
{
    void A()
    {
        _ = string.Join(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ValidateAsync();
    }

    [Theory]
    [InlineData(@""","", new object[0]")]
    [InlineData(@""","", new string[0]")]
    [InlineData(@""","", new string[0], 0, 1")]
    [InlineData(@""","", Enumerable.Empty<object>()")]
    [InlineData(@""","", Enumerable.Empty<string>()")]
    public async Task Join_NoReport_netstandard2_0(string method)
    {
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test
{
    void A()
    {
        _ = string.Join(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.NetStandard2_0)
              .ValidateAsync();
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
    public async Task Join_NoReport(string method)
    {
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Test
{
    void A()
    {
        _ = string.Join(" + method + @");
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.Net6_0)
              .ValidateAsync();
    }
}
