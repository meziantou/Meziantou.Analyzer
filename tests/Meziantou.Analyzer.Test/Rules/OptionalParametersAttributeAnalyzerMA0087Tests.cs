using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptionalParametersAttributeAnalyzerMA0087Tests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptionalParametersAttributeAnalyzer>(id: "MA0087");
    }

    [Fact]
    public async Task MissingOptionalAttribute()
    {
        const string SourceCode = @"using System.Runtime.InteropServices;
class Test
{
    void A([DefaultParameterValue(10)]int [|a|])
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
        const string SourceCode = @"using System.Runtime.InteropServices;
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
    public async Task OptionalAttribute()
    {
        const string SourceCode = @"using System.Runtime.InteropServices;
class Test
{
    void A([Optional]int a)
    {
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
