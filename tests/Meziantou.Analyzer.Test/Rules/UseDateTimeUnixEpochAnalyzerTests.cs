using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;
public class UseDateTimeUnixEpochAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net7_0)
            .WithAnalyzer<UseDateTimeUnixEpochAnalyzer>();
    }

    [Theory]
    [InlineData("new DateTime(1970, 1, 1)")]
    [InlineData("new DateTime(1970, 1, 1, 0,0,0)")]
    [InlineData("new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)")]
    [InlineData("new DateTime(621355968000000000)")]
    [InlineData("new DateTime(621355968000000000, DateTimeKind.Utc)")]
    public async Task UnixEpoch_DateTime(string code)
    {
        var sourceCode = $$"""
using System;
class ClassTest
{
   void Test()
   {
       _ = [||]{{code}};
   }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }
    
    [Theory]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch)")]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(621355968000000000, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.Zero)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, default(TimeSpan))")]
    public async Task UnixEpoch_DateTimeOffset(string code)
    {
        var sourceCode = $$"""
using System;
class ClassTest
{
   void Test()
   {
       _ = [||]{{code}};
   }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("new DateTime(1971, 1, 1)")]
    [InlineData("new DateTime(1970, 1, 1, 0,0,1)")]
    [InlineData("new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)")]
    [InlineData("new DateTime(621355968000000001)")]
    [InlineData("new DateTime(621355968000000000, DateTimeKind.Local)")]
    public async Task NonUnixEpoch_DateTime(string code)
    {
        var sourceCode = $$"""
using System;
class ClassTest
{
   void Test()
   {
       _ = {{code}};
   }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("new DateTimeOffset(DateTime.MinValue)")]
    [InlineData("new DateTimeOffset(DateTime.UnixEpoch, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(621355968000000000, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.MinValue)")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.FromMinutes(1))")]
    [InlineData("new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, 0, TimeSpan.FromHours(-1))")]
    public async Task NonUnixEpoch_DateTimeOffset(string code)
    {
        var sourceCode = $$"""
using System;
class ClassTest
{
   void Test()
   {
       _ = {{code}};
   }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonUnixEpoch_DateTime_OldFramework()
    {
        var sourceCode = """
using System;
class ClassTest
{
   void Test()
   {
       _ = new DateTime(1970, 1, 1);
   }
}
""";
        await CreateProjectBuilder()
              .WithSourceCode(sourceCode)
              .WithTargetFramework(TargetFramework.NetStandard2_0)
              .ValidateAsync();
    }
}
