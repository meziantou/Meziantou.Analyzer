using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseDateTimeUnixEpochAnalyzer,
    Meziantou.Analyzer.Rules.UseDateTimeUnixEpochFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public class UseDateTimeUnixEpochAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("new DateTime(1970, 1, 1)")]
    [InlineData("new DateTime(1970, 1, 1, 0,0,0)")]
    [InlineData("new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)")]
    [InlineData("new DateTime(621355968000000000)")]
    [InlineData("new DateTime(621355968000000000, DateTimeKind.Utc)")]
    public Task UnixEpoch_DateTime(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = {|MA0113:{{code}}|};
               }
            }
            """;
        test.FixedCode = """
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = DateTime.UnixEpoch;
               }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch)")]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(621355968000000000, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, default(TimeSpan))")]
    public Task UnixEpoch_DateTimeOffset(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = {|MA0114:{{code}}|};
               }
            }
            """;
        test.FixedCode = """
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = DateTimeOffset.UnixEpoch;
               }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new DateTime(1971, 1, 1)")]
    [InlineData("new DateTime(1970, 1, 1, 0, 0, 1)")]
    [InlineData("new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)")]
    [InlineData("new DateTime(621355968000000001)")]
    [InlineData("new DateTime(621355968000000000, DateTimeKind.Local)")]
    public Task NonUnixEpoch_DateTime(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = {{code}};
               }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new DateTimeOffset(DateTime.MinValue)")]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(621355968000000000, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.FromMinutes(1))")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.FromHours(-1))")]
    public Task NonUnixEpoch_DateTimeOffset(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = {{code}};
               }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonUnixEpoch_DateTime_OldFramework()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.TestCode = """
            using System;
            class ClassTest
            {
               void Test()
               {
                   _ = new DateTime(1970, 1, 1);
               }
            }
            """;

        return test.RunAsync();
    }
}
