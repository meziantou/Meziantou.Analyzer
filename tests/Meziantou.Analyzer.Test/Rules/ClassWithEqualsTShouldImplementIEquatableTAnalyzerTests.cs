using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class ClassWithEqualsTShouldImplementIEquatableTAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ClassWithEqualsTShouldImplementIEquatableTAnalyzer>();
        }

        [Theory]
        [InlineData("public bool Equals(Test other) { return false; }", true)]
        [InlineData("static public bool Equals(Test other) { return false; }", false)]
        [InlineData("private bool Equals(Test other) { return false; }", false)]
        [InlineData("public bool Equals(int other) { return false; }", false)]
        [InlineData("public int Equals(Test other) { return 1; }", false)]
        [InlineData("public void Equals(Test other) {}", false)]
        [InlineData("public bool EqualsTo(Test other) { return false; }", false)]
        public async Task Test_ClassDoesNotImplementIEquatableT(string method, bool expectDiagnostic)
        {
            var className = expectDiagnostic ? "[|Test|]" : "Test";
            var sourceCode = $@"using System;
class {className}
{{
    {method}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsIEquatableT_NoDiagnosticReported()
        {
            const string SourceCode = @"using System;
class Test : IEquatable<Test>
{
    public string Name { get; set; }
    public bool Equals(Test other)
    {
        if (other == null)
            return false;
        if (string.Equals(Name, other.Name, StringComparison.Ordinal))
            return true;
        return false;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
