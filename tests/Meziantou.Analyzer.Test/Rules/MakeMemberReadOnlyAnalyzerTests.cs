using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MakeMemberReadOnlyAnalyzer,
    Meziantou.Analyzer.Rules.MakeMemberReadOnlyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MakeMemberReadOnlyAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();

        // The rule is reported by a compilation action, so the diagnostic is not local to the syntax tree,
        // which the testing library rejects for a code fix by default
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task CannotBeReadOnly_CSharp7()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp7_3;
        test.TestCode = """
            struct Test
            {
                void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_Class()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                int A => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_ReadOnlyStruct()
    {
        var test = CreateTest();
        test.TestCode = """
            readonly struct Test
            {
                int A => throw null;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_Constructor()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                Test(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_StaticConstructor()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                static Test() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_LocalFunction()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_Delegate()
    {
        var test = CreateTest();
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_ReadOnlyStructMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                readonly void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_ReadOnlyStructProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                readonly int A => a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_Events()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                event System.Action<System.EventArgs> MyEvent;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_StaticMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                static void A() { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_StaticProperty()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                static int A => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_ReadOnlyStructPropertyGetter()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                int A
                {
                    readonly get => a;
                    set => a = 0;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_SetThis()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                void A() => this = default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_SetField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                void A() => a = 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_MethodBlock_SetField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                void A() { a = 0; }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_CallNonReadOnlyMember()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                void A() => a = 0;

                void B() => A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_MethodReferenceField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;

                int [|A|]() => a;
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int a;

                readonly int A() => a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_PropertyGetOnlyReferenceField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;

                int [|A|] => a;
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int a;

                readonly int A => a;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_PropertyFullGetterAndSetterReferenceField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;

                int A
                {
                    [|get|] => a;
                    [|set|] { }
                }
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int a;

                readonly int A
                {
                    get => a;
                    set { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_PropertyFullSetterReferenceField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;

                int A
                {
                    readonly get => a;
                    [|set|] { }
                }
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int a;

                readonly int A
                {
                    get => a;
                    set { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_PropertyFullGetterReferenceField()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                int A
                {
                    [|get|] => a;
                    set => a = value;
                }
            }
            """;
        // Roslyn 4.8 inserts CRLF where the newer versions insert LF, and the testing library
        // compares the text of the fixed code exactly
#if ROSLYN_4_14_OR_GREATER
        test.FixedCode = """
            struct Test
            {
                int a;
                int A
                {
                    readonly get => a;
                    set => a = value;
                }
            }
            """;
#endif

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_SetArrayValue()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int[] a;

                int A
                {
                    [|set|] => a[0] = value;
                }
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int[] a;

                readonly int A
                {
                    set => a[0] = value;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_CallReadOnlyMember()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;
                readonly void A() { }

                void [|B|]() => A();
            }
            """;
        test.FixedCode = """
            struct Test
            {
                int a;
                readonly void A() { }

                readonly void B() => A();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CanBeReadOnly_Event()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                public event System.Action<System.EventArgs> [|Event1|]
                {
                    add { }
                    remove { }
                }
            }
            """;
        test.FixedCode = """
            struct Test
            {
                public readonly event System.Action<System.EventArgs> Event1
                {
                    add { }
                    remove { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_EventWithOnlyOneReadOnlyAccessor()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int a;

                public event System.Action<System.EventArgs> Event1
                {
                    add { }
                    remove { a = 0; }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_ReadOnlyEvent()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                public readonly event System.Action<System.EventArgs> Event1
                {
                    add { }
                    remove { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadOnly_CallNonReadOnlyMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int _a;

                void A() => _a = 1;
                void B() => A(); // Should not be readonly (CS8656)
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadonly_CallNonReadOnlyPropertyGetFromMethod()
    {
        var test = CreateTest();
        test.TestCode = """
            struct Test
            {
                int _a;

                int A { get { _a = 1; return 0; } }
                void B() => _ = A; // Should not be readonly (CS8656)
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CannotBeReadonly_AccessNonReadOnlyMember()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task RefFixedMember()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
        test.TestCode = """
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
            """;

        return test.RunAsync();
    }
}
