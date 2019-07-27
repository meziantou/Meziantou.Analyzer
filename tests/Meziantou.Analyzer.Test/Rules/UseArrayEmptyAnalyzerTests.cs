using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseArrayEmptyAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseArrayEmptyAnalyzer>()
                .WithCodeFixProvider<UseArrayEmptyFixer>();
        }

        [Theory]
        [InlineData("new int[0]")]
        [InlineData("new int[] { }")]
        public async System.Threading.Tasks.Task EmptyArray_ShouldReportErrorAsync(string code)
        {
            await CreateProjectBuilder()
                  .WithSourceCode($@"
class TestClass
{{
    void Test()
    {{
        var a = [||]{code};
    }}
}}")
                  .ShouldFixCodeWith(@"
class TestClass
{
    void Test()
    {
        var a = System.Array.Empty<int>();
    }
}")
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("new int[1]")]
        [InlineData("new int[] { 0 }")]
        public async System.Threading.Tasks.Task NonEmptyArray_ShouldReportErrorAsync(string code)
        {
            await CreateProjectBuilder()
                  .WithSourceCode($@"
class TestClass
{{
    void Test()
    {{
        var a = {code};
    }}
}}")
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task Length_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"
class TestClass
{
    void Test()
    {
        int length = 0;
        var a = new int[length];
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task ParamsMethod_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"
public class TestClass
{
    public void Test(params string[] values)
    {
    }

    public void CallTest()
    {
        Test();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task EmptyArrayInAttribute_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"
[Test(new int[0])]
class TestAttribute : System.Attribute
{
    public TestAttribute(int[] data) { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
