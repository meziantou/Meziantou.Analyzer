using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotDeclareStaticMembersOnGenericTypesTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<DoNotDeclareStaticMembersOnGenericTypes>();
    }

    [Fact]
    public async Task StaticMembersInNonGenericClass()
    {
        const string SourceCode = @"
public class Test
{
    public static string field;
    public static string Prop => throw null;
    public static string Method() => throw null;

    public string field2;
    public string Prop2 => throw null;
    public string Method2() => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonStaticMembersInGenericClass()
    {
        const string SourceCode = @"
public class Test<T>
{
    public string field2;
    public string Prop2 => throw null;
    public string Method2() => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMembers_Field()
    {
        const string SourceCode = @"
public class Test<T>
{
    public static string [||]field;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMembers_Property()
    {
        const string SourceCode = @"
public class Test<T>
{
    public static string [||]Prop => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMembers_Method()
    {
        const string SourceCode = @"
public class Test<T>
{
    public static string [||]Method() => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task StaticMembers_Operator()
    {
        const string SourceCode = @"
public class Test<T>
{
    public static implicit operator Test<T>(int i) => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task Const()
    {
        const string SourceCode = @"
public class Test<T>
{    
    public const string PasswordlessSignInPurpose = ""PasswordlessSignIn"";
}";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task NonPublicStaticMembers()
    {
        const string SourceCode = @"
public class Test<T>
{
    internal protected static string Method1() => throw null;
    protected static string Method2() => throw null;
    private protected static string Method3() => throw null;
    internal static string Method4() => throw null;
    private static string Method5() => throw null;
}";
        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

#if CSHARP11_OR_GREATER
    [Fact]
    public async Task StaticAbstract()
    {
        const string SourceCode = """"
public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
{
    static abstract TSelf Create();
}
"""";
        await CreateProjectBuilder()
              .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
              .WithTargetFramework(TargetFramework.Net7_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
#endif
}
