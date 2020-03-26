using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class AvoidComparisonWithBoolConstantAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<AvoidComparisonWithBoolConstantAnalyzer>()
                .WithCodeFixProvider<AvoidComparisonWithBoolConstantFixer>();
        }

        [Theory]
        [InlineData("==", "true", "")]
        [InlineData("==", "false", "!")]
        [InlineData("!=", "true", "!")]
        [InlineData("!=", "false", "")]
        public async Task ComparingVariableWithBoolLiteral_KeepsVariable(string equalityOperator, string boolLiteral, string expectedPrefix)
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
        public async Task ComparingBoolLiteralWithExpression_KeepsExpression(string boolLiteral, string equalityOperator, string expectedPrefix)
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

        [Theory]
        [InlineData("==", "true", "")]
        [InlineData("==", "false", "!")]
        [InlineData("!=", "true", "!")]
        [InlineData("!=", "false", "")]
        public async Task ComparingVariableWithBoolConstant_KeepsVariable(string equalityOperator, string constBool, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant = {constBool};
        bool a = false;
        _ = a [|{equalityOperator}|] MyConstant;
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant = {constBool};
        bool a = false;
        _ = {expectedPrefix}a;
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("!=", "true", "!")]
        [InlineData("==", "MyConstant2", "")]
        public async Task ComparingBoolConstantsAndLiterals_KeepsSecondOperand(string equalityOperator, string secondOperand, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant1 = true;
        const bool MyConstant2 = false;
        _ = MyConstant1 [|{equalityOperator}|] {secondOperand};
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant1 = true;
        const bool MyConstant2 = false;
        _ = {expectedPrefix}{secondOperand};
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ComparingNullableBoolVariableWithBoolLiteral_NoDiagnosticReported()
        {
            var originalCode = @"
class TestClass
{
    void Test()
    {
        bool? a = true;
        if (a == true)
        {
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task NotComparingBoolVariable_NoDiagnosticReported()
        {
            var originalCode = @"
class TestClass
{
    void Test()
    {
        bool a = true;
        if (a)
        {
        }
        if (!a)
        {
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }
    }
}
