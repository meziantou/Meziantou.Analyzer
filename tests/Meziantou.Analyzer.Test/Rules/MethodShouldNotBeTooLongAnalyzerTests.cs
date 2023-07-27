using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MethodShouldNotBeTooLongAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<MethodShouldNotBeTooLongAnalyzer>();
    }

    [Fact]
    public async Task TooLongMethod_Statements()
    {
        const string SourceCode = @"
public class Test
{
    void [||]Method()
    {
        var a = 0;var b = 0;
        void A(){var c = 0;}
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_statements_per_method", "2")
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidMethod_Statements()
    {
        const string SourceCode = @"
public class Test
{
    void Method()
    {
        var a = 0;var b = 0;var c = 0;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_statements_per_method", "3")
              .ValidateAsync();
    }

    [Fact]
    public async Task TooLongMethod_Lines()
    {
        const string SourceCode = @"
public class Test
{
    void [||]Method()
    {
        var a = 0;var d = 0;
        var b = 0;var e = 0;
        var c = 0;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_lines_per_method", "2")
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidMethod_Lines()
    {
        const string SourceCode = @"
public class Test
{
    void Method()
    {
        var a = 0;var d = 0;
        var b = 0;var e = 0;
        var c = 0;
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_lines_per_method", "4")
              .ValidateAsync();
    }

    [Fact]
    public async Task TooLong_SkipLocalFunction_False()
    {
        const string SourceCode = @"
public class Test
{
    void [||]Method()
    {
        var a = 0;
        var b = 0;
        var c = 0;

        void A()
        {
            void B() { }
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_lines_per_method", "5")
              .AddAnalyzerConfiguration("MA0051.skip_local_functions", "false")
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidMethod_SkipLocalFunction_True()
    {
        const string SourceCode = @"
public class Test
{
    void Method()
    {
        var a = 0;
        var b = 0;
        var c = 0;

        void A()
        {
            void B() { }
        }
    }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_lines_per_method", "5")
              .AddAnalyzerConfiguration("MA0051.skip_local_functions", "true")
              .ValidateAsync();
    }

    [Fact]
    public async Task ValidMethod_Statements_SkipLocalFunction()
    {
        const string SourceCode = """
            public class Test
            {
                void Method()
                {
                    var a = 0;
                    var b = 0;

                    void A()
                    {
                        _ = "";

                        void B() { }
                    }
                }
            }
            """;
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0051.maximum_lines_per_method", "-1")
              .AddAnalyzerConfiguration("MA0051.maximum_statements_per_method", "2")
              .AddAnalyzerConfiguration("MA0051.skip_local_functions", "true")
              .ValidateAsync();
    }

    [Fact]
    public void CountStatement_ForLoop()
    {
        const string SourceCode = @"
for (int a = 0; i < 0; i++)
{
    throw null;
}";

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountStatement_LocalFunction()
    {
        const string SourceCode = @"
throw null;

void B()
{
    throw null;
}
";

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    [Fact]
    public void CountStatement_If()
    {
        const string SourceCode = @"
if (true)
{
    throw null;
}
";

        var count = CountStatements(SourceCode);
        Assert.Equal(2, count);
    }

    private static int CountStatements(string code)
    {
        var tree = CSharpSyntaxTree.ParseText("void A(){ " + code + " }");

        var root = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
        return MethodShouldNotBeTooLongAnalyzer.CountStatements(default, root);
    }
}
