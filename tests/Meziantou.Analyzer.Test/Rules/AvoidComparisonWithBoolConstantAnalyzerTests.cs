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
        [InlineData("==", "true", null)]
        [InlineData("==", "false", "!")]
        [InlineData("!=", "true", "!")]
        [InlineData("!=", "false", null)]
        public async Task ComparingVariableWithBoolLiteral_RemovesComparisonAndKeepsVariable(string op, string literal, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        var value = false;
        if (value [|{op}|] {literal})
        {{
        }}
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        var value = false;
        if ({expectedPrefix}value)
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
        [InlineData("true", "==", "(GetSomeNumber() == 15)", "GetSomeNumber() == 15")]
        [InlineData("false", "==", "(GetSomeNumber() == 15)", "!(GetSomeNumber() == 15)")]
        [InlineData("true", "!=", "(GetSomeNumber() == 15)", "!(GetSomeNumber() == 15)")]
        [InlineData("false", "!=", "(GetSomeNumber() == 15)", "GetSomeNumber() == 15")]
        public async Task ComparingBoolLiteralWithExpression_RemovesComparisonAndKeepsExpression(string literal, string op, string originalExpression, string modifiedExpression)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        var value = {literal} [|{op}|] {originalExpression};
        int GetSomeNumber() => 12;
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        var value = {modifiedExpression};
        int GetSomeNumber() => 12;
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("==", "true", null)]
        [InlineData("==", "false", "!")]
        [InlineData("!=", "true", "!")]
        [InlineData("!=", "false", null)]
        public async Task ComparingVariableWithBoolConstant_RemovesComparisonAndKeepsVariable(string op, string constBool, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant = {constBool};
        bool value = false;
        _ = value [|{op}|] MyConstant;
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant = {constBool};
        bool value = false;
        _ = {expectedPrefix}value;
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("!=", "true", "!")]
        [InlineData("==", "MyConstant2", null)]
        public async Task ComparingBoolConstantsAndLiterals_RemovesComparisonAndKeepsRightOperand(string op, string rightOperand, string expectedPrefix)
        {
            var originalCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant1 = true;
        const bool MyConstant2 = false;
        _ = MyConstant1 [|{op}|] {rightOperand};
    }}
}}";
            var modifiedCode = $@"
class TestClass
{{
    void Test()
    {{
        const bool MyConstant1 = true;
        const bool MyConstant2 = false;
        _ = {expectedPrefix}{rightOperand};
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
        bool? value = true;
        if (value == true)
        {
        }
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("dynamicValue == true")]
        [InlineData("true == AsDynamic().MaybeBoolean")]
        [InlineData("((dynamic)this.TrulyBoolean) == true")]
        public async Task ComparingDynamicVariableWithBoolLiteral_NoDiagnosticReported(string expression)
        {
            var originalCode = $@"
class TestClass
{{
    public bool? MaybeBoolean {{ get; set; }}
    public bool  TrulyBoolean {{ get; set; }}

    public dynamic AsDynamic() {{ return this; }}

    void Test()
    {{
        dynamic dynamicValue = true;
        if ({expression})
        {{
        }}
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .AddDynamicTypeSupport()
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
        bool value = true;
        if (value)
        {
        }
        if (!value)
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
