using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ConstructorArgumentParametersShouldExistInConstructorsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ConstructorArgumentParametersShouldExistInConstructorsAnalyzer>()
            .WithTargetFramework(TargetFramework.Net4_8);
    }

    [Fact]
    public async Task WrongParameterName()
    {
        const string SourceCode = @"
public class MyExtension
{
    public MyExtension() { }

    public MyExtension(object value1)
    {
        Value1 = value1;
    }

    [[|System.Windows.Markup.ConstructorArgument(""value2"")|]]
    public object Value1 { get; set; }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task GoodParameterName()
    {
        const string SourceCode = @"
public class MyExtension
{
    public MyExtension() { }

    public MyExtension(object value1)
    {
        Value1 = value1;
    }

    [System.Windows.Markup.ConstructorArgument(""value1"")]
    public object Value1 { get; set; }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
