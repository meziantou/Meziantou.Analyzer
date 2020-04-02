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
                .WithAnalyzer<ClassWithEqualsTShouldImplementIEquatableTAnalyzer>()
                .WithCodeFixProvider<ClassWithEqualsTShouldImplementIEquatableTFixer>();
        }

        [Fact]
        public async Task Test_ClassImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"
class [|Test|]
{
    public bool Equals(Test other) => throw null;
}";
            var modifiedCode = @"
class Test : System.IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"
struct [|Test|]     //  This comment stays
{
    public bool Equals(Test other) => throw null;
}";
            var modifiedCode = @"
struct Test : System.IEquatable<Test>     //  This comment stays
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("static public bool Equals(Test other)")]
        [InlineData("private bool Equals(Test other)")]
        [InlineData("public bool Equals(int other)")]
        [InlineData("public int Equals(Test other)")]
        [InlineData("public void Equals(Test other)")]
        [InlineData("public bool EqualsTo(Test other)")]
        public async Task Test_ClassImplementsNoInterfaceAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
        {
            var originalCode = $@"
class Test
{{
    {methodSignature} => throw null;
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Theory]
        [InlineData("static public bool Equals(Test other)")]
        [InlineData("private bool Equals(Test other)")]
        [InlineData("public bool Equals(int other)")]
        [InlineData("public int Equals(Test other)")]
        [InlineData("public void Equals(Test other)")]
        [InlineData("public bool EqualsTo(Test other)")]
        public async Task Test_StructImplementsNoInterfaceAndProvidesIncompatibleEqualsMethod_NoDiagnosticReported(string methodSignature)
        {
            var originalCode = $@"
struct Test
{{
    {methodSignature} => throw null;
}}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
        {
            var originalCode = @"using System;
class Test : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsSystemIEquatableWithTOfRightType_NoDiagnosticReported()
        {
            var originalCode = @"using System;
struct Test : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"using System;
class [|Test|] : IEquatable<string>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            var modifiedCode = @"using System;
class Test : IEquatable<string>, IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsSystemIEquatableWithTOfWrongTypeButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"using System;
struct [|Test|] : IEquatable<string>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            var modifiedCode = @"using System;
struct Test : IEquatable<string>, IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
    public bool Equals(string other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_ClassImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"
interface IEquatable<T> { bool Equals(T other); }
class [|Test|] : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            var modifiedCode = @"
interface IEquatable<T> { bool Equals(T other); }
class Test : IEquatable<Test>, System.IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_StructImplementsWrongIEquatableButProvidesCompatibleEqualsMethod_DiagnosticIsReported()
        {
            var originalCode = @"
interface IEquatable<T> { bool Equals(T other); }
struct [|Test|] : IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            var modifiedCode = @"
interface IEquatable<T> { bool Equals(T other); }
struct Test : IEquatable<Test>, System.IEquatable<Test>
{
    public bool Equals(Test other) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ShouldFixCodeWith(modifiedCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Test_InterfaceDoesNotInheritFromSystemIEquatableButProvidesCompatibleEqualsMethod_NoDiagnosticReported()
        {
            var originalCode = @"
public interface ITest
{
    bool Equals(ITest other);
}";
            await CreateProjectBuilder()
                  .WithSourceCode(originalCode)
                  .ValidateAsync();
        }
    }
}
