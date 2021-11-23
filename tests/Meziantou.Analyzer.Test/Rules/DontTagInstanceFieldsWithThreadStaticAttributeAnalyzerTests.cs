using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DontTagInstanceFieldsWithThreadStaticAttributeAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DontTagInstanceFieldsWithThreadStaticAttributeAnalyzer>();
    }

    [Fact]
    public async Task DontReport()
    {
        const string SourceCode = @"
class Test2
{
    int _a;
    [System.ThreadStatic]
    static int _b;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Report()
    {
        const string SourceCode = @"
class Test2
{
    [System.ThreadStatic]
    int [||]_a;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
