using Meziantou.Analyzer.Test.Harness;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MethodOverridesShouldNotChangeParameterDefaultsAnalyzer,
    Meziantou.Analyzer.Rules.MethodOverridesShouldNotChangeParameterDefaultsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MethodOverridesShouldNotChangeParameterDefaultsAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    private static DiagnosticResult ExpectedChangedDefault(int markupKey, string original, string current) =>
        new DiagnosticResult(RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults, DiagnosticSeverity.Warning)
            .WithLocation(markupKey)
            .WithMessage($"Method overrides should not change default values (original: {original}; current: {current})");

    // The parameters of a referenced assembly have no syntax, unlike the ones of a project of the solution
    private static async Task<MetadataReference> CreateLibraryReferenceAsync(string code)
    {
        var references = await AnalyzerTestDefaults.ReferenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        var compilation = CSharpCompilation.Create(
            "Library",
            [CSharpSyntaxTree.ParseText(code)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    [Fact]
    public Task Interface_ExplicitImplementation()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(int a = 0);
            }

            class Test : ITest
            {
                void ITest.A(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Interface_SameValue()
    {
        var test = CreateTest();
        test.TestCode = """
            interface ITest
            {
                void A(int a = 0);
            }

            class Test : ITest
            {
                public void A(int a = 0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_SameValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0, int b = 1) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|} = 1, int {|#1:b|} = 2) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "'0'", "'1'"));
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(1, "'1'", "'2'"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0, int b = 1) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task New_DifferentValue()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0, int b = 1) { }
            }

            class TestDerived : Test
            {
                public void A(int a = 1, int b = 2) { } // no override
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue_OriginalParameterHasNoDefault()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|} = 1) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "<no default value>", "'1'"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a) { }
            }

            class TestDerived : Test
            {
                public override void A(int a) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_DifferentValue_OverrideParameterHasNoDefault()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                public virtual void A(int a = 0) { }
            }

            class TestDerived : Test
            {
                public override void A(int {|#0:a|}) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "'0'", "<no default value>"));
        test.FixedCode = """
            class Test
            {
                public virtual void A(int a = 0) { }
            }

            class TestDerived : Test
            {
                public override void A(int a = 0) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_BaseParameterFromReferencedAssembly_StructDefaultValue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.IO;
            using System.Threading;
            using System.Threading.Tasks;

            abstract class MyStream : Stream
            {
                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken {|#0:cancellationToken|}) => default;
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "null", "<no default value>"));
        test.FixedCode = """
            using System;
            using System.IO;
            using System.Threading;
            using System.Threading.Tasks;

            abstract class MyStream : Stream
            {
                public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => default;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Override_BaseParameterFromReferencedAssembly_BooleanDefaultValue()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Buffers;

            abstract class MyPool : ArrayPool<int>
            {
                public override void Return(int[] array, bool {|#0:clearArray|}) { }
            }
            """;
        test.ExpectedDiagnostics.Add(ExpectedChangedDefault(0, "'False'", "<no default value>"));
        test.FixedCode = """
            using System.Buffers;

            abstract class MyPool : ArrayPool<int>
            {
                public override void Return(int[] array, bool clearArray = false) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public async Task Override_BaseParameterFromReferencedAssembly_ConstantDefaultValues()
    {
        var test = CreateTest();
        test.TestState.AdditionalReferences.Add(await CreateLibraryReferenceAsync("""
            namespace Library
            {
                public enum Mode { A, B, @class }

                public abstract class Base
                {
                    public abstract void M(string text = "a\"b", Mode mode = Mode.B, Mode other = (Mode)5, Mode negative = (Mode)(-1), Mode? nullableMode = Mode.@class, char c = 'x', float f = 1.5f, double d = double.NaN, decimal m = 1.5m, long l = 2, int? i = null, string s = null);
                }
            }
            """));
        test.TestCode = """
            using Library;

            class Derived : Base
            {
                public override void M(string {|MA0061:text|}, Mode {|MA0061:mode|}, Mode {|MA0061:other|}, Mode {|MA0061:negative|}, Mode? {|MA0061:nullableMode|}, char {|MA0061:c|}, float {|MA0061:f|}, double {|MA0061:d|}, decimal {|MA0061:m|}, long {|MA0061:l|}, int? {|MA0061:i|}, string {|MA0061:s|}) { }
            }
            """;
        test.FixedCode = """
            using Library;

            class Derived : Base
            {
                public override void M(string text = "a\"b", Mode mode = Mode.B, Mode other = (Mode)5, Mode negative = (Mode)(-1), Mode? nullableMode = Mode.@class, char c = 'x', float f = 1.5F, double d = double.NaN, decimal m = 1.5M, long l = 2L, int? i = null, string s = null) { }
            }
            """;

        await test.RunAsync();
    }
}
