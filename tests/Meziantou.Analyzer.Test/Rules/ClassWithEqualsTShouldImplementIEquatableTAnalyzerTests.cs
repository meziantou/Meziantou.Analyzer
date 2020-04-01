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

        [Fact]
        public async Task Test_ClassDoesNotImplementIEquatableAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var sourceCode = @"
class [|Test|]
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructDoesNotImplementIEquatableAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var sourceCode = @"
struct [|Test|]
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("static public bool Equals(Test other)")]
        [InlineData("private bool Equals(Test other)")]
        [InlineData("public bool Equals(int other)")]
        [InlineData("public int Equals(Test other)")]
        [InlineData("public void Equals(Test other)")]
        [InlineData("public bool EqualsTo(Test other)")]
        public async Task Test_ClassDoesNotImplementIEquatableAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
        {
            var sourceCode = $@"
class Test
{{
    {methodSignature} => throw null;
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("static public bool Equals(Test other)")]
        [InlineData("private bool Equals(Test other)")]
        [InlineData("public bool Equals(int other)")]
        [InlineData("public int Equals(Test other)")]
        [InlineData("public void Equals(Test other)")]
        [InlineData("public bool EqualsTo(Test other)")]
        public async Task Test_StructDoesNotImplementIEquatableAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
        {
            var sourceCode = $@"
struct Test
{{
    {methodSignature} => throw null;
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(sourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
        {
            const string SourceCode = @"using System;
class Test : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
        {
            const string SourceCode = @"using System;
struct Test : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            const string SourceCode = @"using System;
class [|Test|] : IEquatable<string>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            const string SourceCode = @"using System;
struct [|Test|] : IEquatable<string>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            const string SourceCode = @"
interface IEquatable<T> { bool Equals(T other); }
class [|Test|] : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            const string SourceCode = @"
interface IEquatable<T> { bool Equals(T other); }
struct [|Test|] : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_InterfaceDoesNotImplementSystemIEquatableButProvidesCompatibleEqualsMethod_NoDiagnosticReported()
        {
            const string SourceCode = @"
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
