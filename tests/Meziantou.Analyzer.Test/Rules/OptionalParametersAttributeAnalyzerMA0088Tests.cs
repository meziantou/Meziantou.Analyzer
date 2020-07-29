using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class OptionalParametersAttributeAnalyzerMA0088Tests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<OptionalParametersAttributeAnalyzer>(id: "MA0088");
        }

        [Fact]
        public async Task DefaultParameterValue()
        {
            const string SourceCode = @"
using System.ComponentModel;
using System.Runtime.InteropServices;

class Test
{
    void A([Optional, DefaultParameterValue(10)]int a)
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task DefaultValue()
        {
            const string SourceCode = @"
using System.ComponentModel;
using System.Runtime.InteropServices;

class Test
{
    void A([DefaultValue(10)]int [|a|])
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task BothAttributes()
        {
            const string SourceCode = @"
using System.ComponentModel;
using System.Runtime.InteropServices;

class Test
{
    void A([Optional, DefaultParameterValue(10), DefaultValue(10)]int a)
    {
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

    }
}
