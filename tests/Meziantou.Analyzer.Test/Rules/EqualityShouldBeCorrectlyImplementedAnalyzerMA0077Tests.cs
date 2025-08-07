using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class EqualityShouldBeCorrectlyImplementedAnalyzerMA0077Tests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<EqualityShouldBeCorrectlyImplementedAnalyzer>()
            .WithCodeFixProvider<EqualityShouldBeCorrectlyImplementedFixer>();
    }

    [Fact]
    public async Task Test_ClassImplementsNoInterfaceAndProvidesCompatibleEqualsMethod_DiagnosticIsReported()
    {
        var originalCode = @"
class BaseClass {}
class [|Test|] : BaseClass
{
    public bool Equals(Test other) => throw null;
}";
        var modifiedCode = @"
class BaseClass {}
class Test : BaseClass, System.IEquatable<Test>
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

#if CSHARP12_OR_GREATER
    [Fact]
    public async Task RefStruct_CSharp12()
    {
        var originalCode = @"
ref struct Test
{
    public bool Equals(Test other) => throw null;
}";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12)
              .WithTargetFramework(Helpers.TargetFramework.Net9_0)
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }
#endif

#if CSHARP13_OR_GREATER
    [Fact]
    public async Task RefStruct_CSharp13()
    {
        var originalCode = @"
ref struct [|Test|]
{
    public bool Equals(Test other) => throw null;
}";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp13)
              .WithTargetFramework(Helpers.TargetFramework.Net9_0)
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }
#endif

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
    public async Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNullableType_DiagnosticIsReported()
    {
        var originalCode = @"
#nullable enable
class [|Test|]
{
    public bool Equals(Test? other) => throw null;
}";
        var modifiedCode = @"
#nullable enable
class Test : System.IEquatable<Test?>
{
    public bool Equals(Test? other) => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Test_ClassImplementsNoInterfaceButProvidesEqualsMethodOnNonNullableType_DiagnosticIsReported()
    {
        var originalCode = @"
#nullable enable
class [|Test|]
{
    public bool Equals(Test other) => throw null;
}";
        var modifiedCode = @"
#nullable enable
class Test : System.IEquatable<Test>
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
    public override bool Equals(object o) => throw null;
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
    public override bool Equals(object o) => throw null;
    public bool Equals(Test other) => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
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
