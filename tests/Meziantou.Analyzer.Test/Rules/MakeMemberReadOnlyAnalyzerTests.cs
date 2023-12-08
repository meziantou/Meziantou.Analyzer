using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using TestHelper;
using Xunit;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MakeMemberReadOnlyAnalyzerTests
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
    public async Task CannotBeReadOnly_Constructor()
    {
        const string SourceCode = @"
struct Test
{
    Test(int a) { }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CannotBeReadOnly_StaticConstructor()
    {
        const string SourceCode = @"
struct Test
{
    static Test() { }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CannotBeReadOnly_LocalFunction()
    {
        const string SourceCode = @"
struct Test
{
    int a;

    void A()
    {
        a = 0;
        B();

        void B() { }
    }
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CannotBeReadOnly_Delegate()
    {
        const string SourceCode = @"
using System.Linq;
struct Test
{
    int a;

    void A()
    {
        a = 0;
        new int[1].Where(item => item > 0);
    }
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

    [Fact]
    public async Task CannotBeReadonly_CallNonReadOnlyMethod()
    {
        const string SourceCode = @"
struct Test
{
    int _a;

    void A() => _a = 1;
    void B() => A(); // Should not be readonly (CS8656)
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CannotBeReadonly_CallNonReadOnlyPropertyGetFromMethod()
    {
        const string SourceCode = @"
struct Test
{
    int _a;

    int A { get { _a = 1; return 0; } }
    void B() => _ = A; // Should not be readonly (CS8656)
}
";

        await CreateProjectBuilder()
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task CannotBeReadonly_AccessNonReadOnlyMember()
    {
        const string SourceCode = @"
using System;
internal ref struct PathReader
{
    private int _currentSegmentLength;

    public ReadOnlySpan<char> CurrentText { get; private set; }
    public ReadOnlySpan<char> CurrentSegment => CurrentText.Slice(0, CurrentSegmentLength);                  // Should not be readonly
    public ReadOnlySpan<char> CurrentSegment2 { get { return CurrentText.Slice(0, CurrentSegmentLength); } } // Should not be readonly

    public int CurrentSegmentLength
    {
        get
        {
            _currentSegmentLength = CurrentText.Length;
            return _currentSegmentLength;
        }
    }
}
";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task RefFixedMember()
    {
        const string SourceCode = @"
using System;
using System.Runtime.InteropServices;
struct Repro
{
    private unsafe fixed byte bytes[16];

    public unsafe Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref bytes[0], 16);
    }
}
";

        await CreateProjectBuilder()
              .WithTargetFramework(TargetFramework.Net5_0)
              .WithSourceCode(SourceCode)
              .ValidateAsync();
    }
}
