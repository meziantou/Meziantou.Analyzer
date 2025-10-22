using Meziantou.Analyzer.Rules;
using Meziantou.Analyzer.Test.Helpers;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;
public sealed class UseSystemThreadingLockInsteadOfObjectAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithTargetFramework(TargetFramework.Net9_0)
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview)
            .WithAnalyzer<UseSystemThreadingLockInsteadOfObjectAnalyzer>();
    }

    [Fact]
    public async Task Field_CSharp12()
    {

        await CreateProjectBuilder()
            .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7)
            .WithSourceCode("""
                class TypeName
                {
                    string _lock = "dummy";

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

#if CSHARP13_OR_GREATER
    [Fact]
    public async Task Field_NoUsage()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    object _lock = new();
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_NotObject_OnlyLockUsage()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    string _lock = "dummy";

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_OnlyLockUsage()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    object [||]_lock = new();

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_OnlyLockUsage_NET8()
    {

        await CreateProjectBuilder()
            .WithTargetFramework(TargetFramework.Net8_0)
            .WithSourceCode("""
                class TypeName
                {
                    object _lock = new();

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_LockAndOtherUsages()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    object _lock = new();

                    void A() { lock(_lock) { } }
                    void B() { _lock.ToString(); }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_OtherUsagesInDerivedClass()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class BaseClass
                {
                    private protected object _lock = new();

                    void A() { lock(_lock) { } }
                }

                class ChildClass : BaseClass
                {
                    void B() { _lock.ToString(); }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Field_LockInDerivedClass()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class BaseClass
                {
                    private protected object [||]_lock = new();
                }

                class ChildClass : BaseClass
                {
                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [InlineData("public", "protected")]
    [InlineData("public", "public")]
    public async Task Field_Public(string classVisibility, string fieldVisibility)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                {{classVisibility}} class BaseClass
                {
                    {{fieldVisibility}} object _lock = new();

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Theory]
    [InlineData("public", "private")]
    [InlineData("public", "private protected")]
    [InlineData("public", "internal")]
    [InlineData("internal", "public")]
    public async Task Field_Private(string classVisibility, string fieldVisibility)
    {
        await CreateProjectBuilder()
            .WithSourceCode($$"""
                {{classVisibility}} class BaseClass
                {
                    {{fieldVisibility}} object [||]_lock = new();

                    void A() { lock(_lock) { } }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariable_Lock()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    void A()
                    {
                        var [||]o = new object();
                        lock(o) { }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariable_LockAndOtherUsages()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    void A()
                    {
                        var o = new object();
                        lock(o) { }
                        o.ToString();
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariable_Lambda()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    void A()
                    {
                        var [||]o = new object();
                        System.Threading.Tasks.Task.Run(() => { lock(o) { } });
                        lock(o) { }
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task LocalVariable_Lambda_OtherUsage()
    {

        await CreateProjectBuilder()
            .WithSourceCode("""
                class TypeName
                {
                    void A()
                    {
                        var o = new object();
                        System.Threading.Tasks.Task.Run(() => { lock(o) { o.ToString(); } });
                        lock(o) { }
                    }
                }
                """)
            .ValidateAsync();
    }
#endif
}
