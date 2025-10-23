using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class ClassMustBeSealedAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<ClassMustBeSealedAnalyzer>()
            .WithCodeFixProvider<ClassMustBeSealedFixer>();
    }

    [Fact]
    public async Task AbstractClass_NoDiagnostic()
    {
        const string SourceCode = @"
abstract class AbstractClass
{
    static void A() { }
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Inherited_Diagnostic()
    {
        const string SourceCode = @"
class Test
{
}

class [||]Test2 : Test
{
}
";

        const string CodeFix = @"
class Test
{
}

sealed class Test2 : Test
{
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task ImplementInterface_Diagnostic()
    {
        const string SourceCode = @"
interface ITest
{
}

class [||]Test : ITest
{
}
";
        const string CodeFix = @"
interface ITest
{
}

sealed class Test : ITest
{
}
";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMethodAndConstField_NotReported()
    {
        const string SourceCode = @"
public class Test
{
    const int a = 10;
    static void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMethodAndConstFieldWithEditorConfigTrue_Diagnostic()
    {
        const string SourceCode = @"
public class [||]Test
{
    const int a = 10;
    static void A() { }
}";
        const string CodeFix = @"
public sealed class Test
{
    const int a = 10;
    static void A() { }
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0053.public_class_should_be_sealed", "true")
              .ShouldFixCodeWith(CodeFix)
              .ValidateAsync();
    }

    [Fact]
    public async Task GenericBaseClass()
    {
        const string SourceCode = @"
internal class Base<T>
{
}

internal sealed class Child : Base<int>
{
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Exception()
    {
        const string SourceCode = @"
internal class SampleException : System.Exception
{
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Exception_ConfigEnabled()
    {
        const string SourceCode = @"
internal class [||]SampleException : System.Exception
{
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .AddAnalyzerConfiguration("MA0053.exceptions_should_be_sealed", "true")
              .ValidateAsync();
    }

    [Fact]
    public async Task VirtualMember()
    {
        const string SourceCode = @"
internal class SampleException
{
    protected virtual void A() => throw null;
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task VirtualMember_EditorConfig()
    {
        const string SourceCode = @"
internal class [||]SampleException
{
    protected virtual void A() => throw null;
}";

        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0053.class_with_virtual_member_shoud_be_sealed", "true")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task ComImport()
    {
        const string SourceCode = @"
[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid(""1A894A19-2FCD-4F87-A5A2-83C64F9FA833"")]
internal class SampleException
{
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task TopLevelStatement_9()
    {
        const string SourceCode = @"
System.Console.WriteLine();
";

        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp9)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task BenchmarkDotNetAttributes()
    {
        const string SourceCode = @"
using BenchmarkDotNet.Attributes;
internal class Test
{
    [Benchmark(Baseline = true)]
    public void A()
    {
    }
}
";

        await CreateProjectBuilder()
              .AddNuGetReference("BenchmarkDotNet.Annotations", "0.13.2", "lib/netstandard2.0/")
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Record()
    {
        await CreateProjectBuilder()
              .AddAnalyzerConfiguration("MA0053.class_with_virtual_member_shoud_be_sealed", "true")
              .WithSourceCode("""
                internal record [||]Sample();
                """)
              .ShouldFixCodeWith("""
                internal sealed record Sample();
                """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("private protected")]
    public async Task ClassWithPrivateCtor(string visibility)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                public class [||]Sample
                {
                    {{visibility}} Sample() { }
                }
                """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("private")]
    [InlineData("internal")]
    [InlineData("private protected")]
    public async Task ClassWithMultiplePrivateCtors(string visibility)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                public class [||]Sample
                {
                    private Sample(int a) { }
                    {{visibility}} Sample() { }
                }
                """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    public async Task ClassWithPublicCtor(string visibility)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                public class Sample
                {
                    {{visibility}} Sample() { }
                }
                """)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    public async Task ClassWithPrivateAndPublicCtor(string visibility)
    {
        await CreateProjectBuilder()
              .WithSourceCode($$"""
                public class Sample
                {
                    private Sample(int a) { }
                    {{visibility}} Sample() { }
                }
                """)
              .ValidateAsync();
    }

#if CSHARP10_OR_GREATER
    [Fact]
    public async Task TopLevelStatement_10()
    {
        const string SourceCode = @"
System.Console.WriteLine();
";

        await CreateProjectBuilder()
              .WithOutputKind(Microsoft.CodeAnalysis.OutputKind.ConsoleApplication)
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp10)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif
}
