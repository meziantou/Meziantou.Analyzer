using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseCountInsteadOfAnyTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny);
    }

    [Fact]
    public async Task Any_List()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = new System.Collections.Generic.List<int>();
        _ = [|collection.Any()|];
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Any_Array()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = new int[10];
        _ = [|collection.Any()|];
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Any_HashSet()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = new System.Collections.Generic.HashSet<int>();
        _ = [|collection.Any()|];
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Any_Dictionary()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = new System.Collections.Generic.Dictionary<int, int>();
        _ = [|collection.Any()|];
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Any_Enumerable()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = Enumerable.Empty<int>();
        _ = collection.Any();
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Any_Expression_Array()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var collection = new int[10];
        _ = collection.Any(i => i > 1);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
