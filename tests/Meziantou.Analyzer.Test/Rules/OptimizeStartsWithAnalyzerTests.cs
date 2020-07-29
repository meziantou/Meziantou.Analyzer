using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class OptimizeStartsWithAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptimizeStartsWithAnalyzer>()
                .WithTargetFramework(TargetFramework.NetStandard2_1);
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
        [InlineData(@"[|""a""|]")]
        [InlineData(@"[|""a""|], StringComparison.Ordinal")]
        [InlineData(@"[|""a""|], StringComparison.CurrentCulture")]
        [InlineData(@"[|""a""|], StringComparison.InvariantCulture")]
        public async Task StartsWith(string method)
        {
            var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.StartsWith(" + method + @");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
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
        [InlineData(@"[|""a""|]")]
        [InlineData(@"[|""a""|], StringComparison.Ordinal")]
        [InlineData(@"[|""a""|], StringComparison.CurrentCulture")]
        [InlineData(@"[|""a""|], StringComparison.InvariantCulture")]
        public async Task EndsWith(string method)
        {
            var sourceCode = @"
using System;
class Test
{
    void A(string str)
    {
        _ = str.EndsWith(" + method + @");
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }
    }
}
