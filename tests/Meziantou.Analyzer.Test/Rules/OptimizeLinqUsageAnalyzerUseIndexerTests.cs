using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseIndexerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.UseIndexerInsteadOfElementAt)
            .WithCodeFixProvider<OptimizeLinqUsageFixer>();
    }

    [Fact]
    public async Task ElementAt_ListAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.[|ElementAt|](10);
        list.ElementAtOrDefault(10);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list[10];
        list.ElementAtOrDefault(10);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use '[]' instead of 'ElementAt()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ElementAt_ArrayAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.[|ElementAt|](10);
        list.ElementAtOrDefault(10);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[10];
        list.ElementAtOrDefault(10);
    }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use '[]' instead of 'ElementAt()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task First_ArrayAsync()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.[|First|]();
        list.First(x=> x == 0);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[0];
        list.First(x=> x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use '[]' instead of 'First()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Last_Array()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list.[|Last|]();
        list.First(x=> x == 0);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new int[5];
        _ = list[list.Length - 1];
        list.First(x=> x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use '[]' instead of 'Last()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task Last_List()
    {
        const string SourceCode = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list.[|Last|]();
        list.First(x=> x == 0);
    }
}
";
        const string CodeFix = @"using System.Linq;
class Test
{
    public Test()
    {
        var list = new System.Collections.Generic.List<int>();
        _ = list[list.Count - 1];
        list.First(x=> x == 0);
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldReportDiagnosticWithMessage("Use '[]' instead of 'Last()'")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }
}
