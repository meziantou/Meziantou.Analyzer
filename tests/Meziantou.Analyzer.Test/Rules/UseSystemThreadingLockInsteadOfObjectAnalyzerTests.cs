using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseSystemThreadingLockInsteadOfObjectAnalyzer,
    Meziantou.Analyzer.Rules.UseSystemThreadingLockInsteadOfObjectFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseSystemThreadingLockInsteadOfObjectAnalyzerTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.LanguageVersion = LanguageVersion.Preview;

        // The rule is reported by a compilation action, so the diagnostic is not local to the syntax tree,
        // which the testing library rejects for a code fix by default
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipLocalDiagnosticCheck;
        return test;
    }

    [Fact]
    public Task Field_CSharp12()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp7;
        test.TestCode = """
            class TypeName
            {
                string _lock = "dummy";

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_NoUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                object _lock = new();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_NotObject_OnlyLockUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                string _lock = "dummy";

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_OnlyLockUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                object {|MA0158:_lock|} = new();

                void A() { lock(_lock) { } }
            }
            """;
        test.FixedCode = """
            class TypeName
            {
                System.Threading.Lock _lock = new();

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("default")]
    [InlineData("new System.Threading.Lock()")]
    public Task Field_OnlyLockUsage_InitializerCompatibleWithLock(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TypeName
            {
                object {|MA0158:_lock|} = {{value}};

                void A() { lock(_lock) { } }
            }
            """;
        test.FixedCode = $$"""
            class TypeName
            {
                System.Threading.Lock _lock = {{value}};

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("CreateGate()")]
    [InlineData("default(object)")]
    [InlineData("(object)null")]
    [InlineData("new object[0]")]
    public Task Field_OnlyLockUsage_InitializerNotCompatibleWithLock(string value)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TypeName
            {
                object _lock = {{value}};

                static object CreateGate() => new object();
                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_OnlyLockUsage_OneInitializerNotCompatibleWithLock()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                object _lock1 = CreateGate(), {|MA0158:_lock2|} = new object();

                static object CreateGate() => new object();
                void A() { lock(_lock1) { } lock(_lock2) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_OnlyLockUsage_NET8()
    {
        var test = CreateTest();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestCode = """
            class TypeName
            {
                object _lock = new();

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_LockAndOtherUsages()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                object _lock = new();

                void A() { lock(_lock) { } }
                void B() { _lock.ToString(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_OtherUsagesInDerivedClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseClass
            {
                private protected object _lock = new();

                void A() { lock(_lock) { } }
            }

            class ChildClass : BaseClass
            {
                void B() { _lock.ToString(); }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_LockInDerivedClass()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseClass
            {
                private protected object {|MA0158:_lock|} = new();
            }

            class ChildClass : BaseClass
            {
                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("public", "protected")]
    [InlineData("public", "public")]
    public Task Field_Public(string classVisibility, string fieldVisibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{classVisibility}} class BaseClass
            {
                {{fieldVisibility}} object _lock = new();

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("public", "private")]
    [InlineData("public", "private protected")]
    [InlineData("public", "internal")]
    [InlineData("internal", "public")]
    public Task Field_Private(string classVisibility, string fieldVisibility)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            {{classVisibility}} class BaseClass
            {
                {{fieldVisibility}} object {|MA0158:_lock|} = new();

                void A() { lock(_lock) { } }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_Lock()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A()
                {
                    var {|MA0158:o|} = new object();
                    lock(o) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_InitializerNotCompatibleWithLock()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A(object value)
                {
                    var o = value;
                    lock(o) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_AssignmentNotCompatibleWithLock()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A(object value)
                {
                    object o = null;
                    o = value;
                    lock(o) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_ForEach()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A(object[] values)
                {
                    foreach (var o in values)
                    {
                        lock(o) { }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_Pattern()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A(object value)
                {
                    if (value is object o)
                    {
                        lock(o) { }
                    }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_LockAndOtherUsages()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A()
                {
                    var o = new object();
                    lock(o) { }
                    o.ToString();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_Lambda()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A()
                {
                    var {|MA0158:o|} = new object();
                    System.Threading.Tasks.Task.Run(() => { lock(o) { } });
                    lock(o) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task LocalVariable_Lambda_OtherUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            class TypeName
            {
                void A()
                {
                    var o = new object();
                    System.Threading.Tasks.Task.Run(() => { lock(o) { o.ToString(); } });
                    lock(o) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_InitializedInConstructor_OnlyLockUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class A
            {
                private readonly object {|MA0158:_lock|};

                public A()
                {
                    _lock = new object();
                }

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;
        test.FixedCode = """
            public sealed class A
            {
                private readonly System.Threading.Lock _lock;

                public A()
                {
                    _lock = new();
                }

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_InitializedInConstructor_LockAndOtherUsages()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class A
            {
                private readonly object _lock;

                public A()
                {
                    _lock = new object();
                }

                public void Run()
                {
                    lock (_lock) { }
                    _lock.ToString();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_InitializedInConstructorFromParameter()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class A
            {
                private readonly object _lock;

                public A(object gate)
                {
                    _lock = gate;
                }

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_InitializedWithCoalesceAssignment()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class A
            {
                private object {|MA0158:_lock|};

                public void Run()
                {
                    _lock ??= new object();
                    lock (_lock) { }
                }
            }
            """;
        test.FixedCode = """
            public sealed class A
            {
                private System.Threading.Lock _lock;

                public void Run()
                {
                    _lock ??= new();
                    lock (_lock) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_InitializedWithObjectInitializer()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class A
            {
                private object _lock;

                public static A Create() => new A { _lock = CreateGate() };
                static object CreateGate() => new object();

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Field_PartialClass_InitializedInConstructorInAnotherDocument()
    {
        var test = CreateTest();
        test.TestCode = """
            partial class A
            {
                private readonly object {|MA0158:_lock|};

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;
        test.TestState.Sources.Add("""
            partial class A
            {
                public A()
                {
                    _lock = new object();
                }
            }
            """);
        test.FixedCode = """
            partial class A
            {
                private readonly System.Threading.Lock _lock;

                public void Run()
                {
                    lock (_lock) { }
                }
            }
            """;
        test.FixedState.Sources.Add("""
            partial class A
            {
                public A()
                {
                    _lock = new();
                }
            }
            """);

        return test.RunAsync();
    }

    [Fact]
    public Task Field_AssignedInDerivedClassInAnotherDocument()
    {
        var test = CreateTest();
        test.TestCode = """
            class BaseClass
            {
                private protected object {|MA0158:_lock|} = new object();

                void A() { lock(_lock) { } }
            }
            """;
        test.TestState.Sources.Add("""
            class ChildClass : BaseClass
            {
                public ChildClass()
                {
                    this._lock = new object();
                }
            }
            """);
        test.FixedCode = """
            class BaseClass
            {
                private protected System.Threading.Lock _lock = new();

                void A() { lock(_lock) { } }
            }
            """;
        test.FixedState.Sources.Add("""
            class ChildClass : BaseClass
            {
                public ChildClass()
                {
                    this._lock = new();
                }
            }
            """);

        return test.RunAsync();
    }

    [Fact]
    public Task StaticField_InitializedInStaticConstructor_OnlyLockUsage()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class B
            {
                private static readonly object {|MA0158:Lock|};

                static B()
                {
                    Lock = new object();
                }

                public void Run()
                {
                    lock (Lock) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticField_InitializedInStaticConstructor_LockAndOtherUsages()
    {
        var test = CreateTest();
        test.TestCode = """
            public sealed class B
            {
                private static readonly object Lock;

                static B()
                {
                    Lock = new object();
                }

                public void Run()
                {
                    lock (Lock) { }
                    Lock.ToString();
                }
            }
            """;

        return test.RunAsync();
    }
}
