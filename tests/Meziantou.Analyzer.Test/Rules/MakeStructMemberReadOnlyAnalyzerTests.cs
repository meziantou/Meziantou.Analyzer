using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules
{
    public class MakeMemberReadOnlyAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MakeMemberReadOnlyAnalyzer>()
                .WithCodeFixProvider<MakeMemberReadOnlyFixer>();
        }

        [Fact]
        public async Task CannotBeReadOnly_CSharp7()
        {
            const string SourceCode = @"
struct Test
{
    void A() { }
}
";

            await CreateProjectBuilder()
                  .WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7_3)
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_Class()
        {
            const string SourceCode = @"
class Test
{
    int A => throw null;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_ReadOnlyStruct()
        {
            const string SourceCode = @"
readonly struct Test
{
    int A => throw null;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_ReadOnlyStructMethod()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    readonly void A() { }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_ReadOnlyStructProperty()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    readonly int A => a;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_Events()
        {
            const string SourceCode = @"
struct Test
{
    event System.Action<System.EventArgs> MyEvent;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_StaticMethod()
        {
            const string SourceCode = @"
struct Test
{
    static void A() { }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_StaticProperty()
        {
            const string SourceCode = @"
struct Test
{
    static int A => 0;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_ReadOnlyStructPropertyGetter()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    int A
    {
        readonly get => a;
        set => a = 0;
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_SetThis()
        {
            const string SourceCode = @"
struct Test
{
    void A() => this = default;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_SetField()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    void A() => a = 0;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_MethodBlock_SetField()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    void A() { a = 0; }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CannotBeReadOnly_CallNonReadOnlyMember()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    void A() => a = 0;

    void B() => A();
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_MethodReferenceField()
        {
            const string SourceCode = @"
struct Test
{
    int a;

    int [||]A() => a;
}
";
            const string CodeFix = @"
struct Test
{
    int a;

    readonly int A() => a;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_PropertyGetOnlyReferenceField()
        {
            const string SourceCode = @"
struct Test
{
    int a;

    int [||]A => a;
}
";
            const string CodeFix = @"
struct Test
{
    int a;

    readonly int A => a;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_PropertyFullGetterAndSetterReferenceField()
        {
            const string SourceCode = @"
struct Test
{
    int a;

    int A
    {
        [||]get => a;
        [||]set { }
    }
}
";
            const string CodeFix = @"
struct Test
{
    int a;

    readonly int A
    {
        get => a;
        set { }
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldBatchFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_PropertyFullSetterReferenceField()
        {
            const string SourceCode = @"
struct Test
{
    int a;

    int A
    {
        readonly get => a;
        [||]set { }
    }
}
";
            const string CodeFix = @"
struct Test
{
    int a;

    readonly int A
    {
        get => a;
        set { }
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_PropertyFullGetterReferenceField()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    int A
    {
        [||]get => a;
        set => a = value;
    }
}
";
            const string CodeFix = @"
struct Test
{
    int a;
    int A
    {
        readonly get => a;
        set => a = value;
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_SetArrayValue()
        {
            const string SourceCode = @"
struct Test
{
    int[] a;

    int A
    {
        [||]set => a[0] = value;
    }
}
";
            const string CodeFix = @"
struct Test
{
    int[] a;

    readonly int A
    {
        set => a[0] = value;
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_CallReadOnlyMember()
        {
            const string SourceCode = @"
struct Test
{
    int a;
    readonly void A() { }

    void [||]B() => A();
}
";
            const string CodeFix = @"
struct Test
{
    int a;
    readonly void A() { }

    readonly void B() => A();
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task CanBeReadOnly_Event()
        {
            const string SourceCode = @"
struct Test
{
    public event System.Action<System.EventArgs> Event1
    {
        [||]add { }
        [||]remove { }
    }
}
";
            const string CodeFix = @"
struct Test
{
    public readonly event System.Action<System.EventArgs> Event1
    {
        add { }
        remove { }
    }
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldBatchFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
