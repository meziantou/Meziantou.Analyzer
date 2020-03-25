using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AvoidComparisonWithBoolLiteralAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AvoidComparisonWithBoolLiteralAnalyzer>()
                .WithCodeFixProvider<AvoidComparisonWithBoolLiteralFixer>();
        }

        [Theory]
        [InlineData("==", "true", "")]
        [InlineData("==", "false", "!")]
        [InlineData("!=", "true", "!")]
        [InlineData("!=", "false", "")]
        public async Task Test_ComparisonWithBoolLiteralRightOperand(string equalityOperator, string boolLiteral, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        var condition = false;
        if (condition [|{equalityOperator}|] {boolLiteral})
        {{
        }}
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        var condition = false;
        if ({expectedPrefix}condition)
        {{
        }}
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("true", "==", "")]
        [InlineData("false", "==", "!")]
        [InlineData("true", "!=", "!")]
        [InlineData("false", "!=", "")]
        public async Task Test_ComparisonWithBoolLiteralLeftOperand(string boolLiteral, string equalityOperator, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        var success = {boolLiteral} [|{equalityOperator}|] (GetSomeNumber() == 15);
        int GetSomeNumber() => 12;
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        var success = {expectedPrefix}(GetSomeNumber() == 15);
        int GetSomeNumber() => 12;
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }
    }
}
