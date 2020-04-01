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
        [InlineData("public bool Equals(Test other)", true)]
        [InlineData("static public bool Equals(Test other)", false)]
        [InlineData("private bool Equals(Test other)", false)]
        [InlineData("public bool Equals(int other)", false)]
        [InlineData("public int Equals(Test other)", false)]
        [InlineData("public void Equals(Test other)", false)]
        [InlineData("public bool EqualsTo(Test other)", false)]
        public async Task Test_ClassDoesNotImplementIEquatableT(string methodSignature, bool expectDiagnostic)
        {
            var className = expectDiagnostic ? "[|Test|]" : "Test";
            var sourceCode = $@"using System;
class {className}
{{
    {methodSignature}
    {{
        throw null;
    }}
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableTWithRightType_NoDiagnosticReported()
        {
            const string SourceCode = @"using System;
class Test : IEquatable<Test>
{
    public bool Equals(Test other)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableTWithWrongType_DiagnosticIsReported()
        {
            const string SourceCode = @"using System;
class [|Test|] : IEquatable<string>
{
    public bool Equals(Test other)
    {
        throw null;
    }

    public bool Equals(string other)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsWrongIEquatableT_DiagnosticIsReported()
        {
            const string SourceCode = @"
interface IEquatable<T> { bool Equals(T other); }
class [|Test|] : IEquatable<Test>
{
    public bool Equals(Test other)
    {
        throw null;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_InterfaceDoesNotImplementSystemIEquatableT_NoDiagnosticReported()
        {
            const string SourceCode = @"using System;
public interface ITest
{
    bool Equals(ITest other);
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
