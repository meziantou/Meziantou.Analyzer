using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class FileNameMustMatchTypeNameAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<FileNameMustMatchTypeNameAnalyzer>();
    }

    [Fact]
    public async Task DoesNotMatchFileName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class [||]Sample
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileNameBeforeDot()
    {
        await CreateProjectBuilder()
              .WithSourceCode("Sample.xaml.cs", @"
class Sample
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test0
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_Generic()
    {
        await CreateProjectBuilder()
              .WithSourceCode(@"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_GenericUsingArity()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0`1.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task DoesMatchFileName_GenericUsingOfT()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0OfT.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task NestedTypeDoesMatchFileName_Ok()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0.cs", @"
class Test0
{
    class Test1
    {
    }
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_MatchType()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{T}.cs", @"
class Test0<T>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_MatchTypes()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey,TValue}.cs", @"
class Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_DoesNotMatchTypeCount()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey}.cs", @"
class [||]Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

    [Fact]
    public async Task Brackets_DoesNotMatchTypeName()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Test0{TKey,TNotSame}.cs", @"
class [||]Test0<TKey, TValue>
{
}")
              .ValidateAsync();
    }

#if ROSLYN_4_4_OR_GREATER
    [Fact]
    public async Task FileLocalTypes()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Dummy.cs", @"
class Dummy
{
}

file class Sample
{
}
")
              .ValidateAsync();
    }

    [Fact]
    public async Task FileLocalTypes_Configuration()
    {
        await CreateProjectBuilder()
              .WithSourceCode(fileName: "Dummy.cs", @"
class Dummy
{
}

file class [||]Sample
{
}
")
              .AddAnalyzerConfiguration("MA0048.exclude_file_local_types", "false")
              .ValidateAsync();
    }
#endif
}
