using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using DiagnosticResult = Microsoft.CodeAnalysis.Testing.DiagnosticResult;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.OptimizeLinqUsageAnalyzer,
    Meziantou.Analyzer.Rules.OptimizeLinqUsageFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseCastInsteadOfSelectTests
{
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.DisabledDiagnostics.Add("MA0020");
        test.DisabledDiagnostics.Add("MA0029");
        test.DisabledDiagnostics.Add("MA0030");
        test.DisabledDiagnostics.Add("MA0031");
        test.DisabledDiagnostics.Add("MA0063");
        test.DisabledDiagnostics.Add("MA0098");
        test.DisabledDiagnostics.Add("MA0112");
        test.DisabledDiagnostics.Add("MA0159");
        return test;
    }


    [Theory]
    [InlineData("source.{|MA0078:Select|}(dt => (BaseType)dt)",
                "source.Cast<BaseType>()")]
    [InlineData("Enumerable.{|MA0078:Select|}(source, dt => (Test.BaseType)dt).FirstOrDefault()",
                "source.Cast<BaseType>().FirstOrDefault()")]
    [InlineData("System.Linq.Enumerable.Empty<DerivedType>().{|MA0078:Select|}(dt => (Gen.IList<string>)dt)",
                            "Enumerable.Empty<DerivedType>().Cast<Gen.IList<string>>()")]
    [InlineData("Enumerable.Range(0, 1).{|MA0078:Select<int, object>|}(i => i)",
                "Enumerable.Range(0, 1).Cast<object>()")]
    [InlineData("source.{|MA0078:Select|}(i => (object?)i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.{|MA0078:Select|}(i => (object)i)",
                "source.Cast<object>()",
                true)]
    [InlineData("source.{|MA0078:Select<DerivedType, object?>|}(i => i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.{|MA0078:Select<DerivedType, object>|}(i => i)",
                "source.Cast<object>()",
                true)]
    public Task OptimizeLinq_WhenSelectorReturnsCastElement_ReplacesSelectByCast(
        string selectInvocation,
        string expectedReplacement,
        bool enableNullable = false)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            #nullable {{(enableNullable ? "enable" : "disable")}}
            using System.Linq;
            using Gen = System.Collections.Generic;

            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{selectInvocation}};
                }
            }
            """;
        test.FixedCode = $$"""
            #nullable {{(enableNullable ? "enable" : "disable")}}
            using System.Linq;
            using Gen = System.Collections.Generic;

            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{expectedReplacement}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("source.Select(dt => dt.Name)")]            // No cast
    [InlineData("source.Select(dt => (object)dt.Name)")]    // Cast of property, not of element itself
    [InlineData("source.Select(dt => dt as BaseType)")]     // 'as' operator should not be replaced by Cast<>
    public Task OptimizeLinq_WhenSelectorDoesNotReturnCastElement_NoDiagnosticReported(string selectInvocation)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            using System.Linq;
            class Test
            {
                class BaseType { public string Name { get; set; } }
                class DerivedType : BaseType {}

                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<DerivedType>();
                    {{selectInvocation}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_ExplicitCastIsRequired()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.Select(item => (byte)item);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public Task OptimizeLinq_UserDefinedImplicitOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select(x => (int)x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static implicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public Task OptimizeLinq_UserDefinedImplicitOperator_ImplicitUse()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select<Foo, int>(x => x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static implicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_UserDefinedExplicitOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Linq;

            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo("1"), new Foo("42") };
                    foreach (var i in foos.Select(x => (int)x))
                    {
                        Console.WriteLine(i);
                    }
                }
            }

            class Foo
            {
                private readonly string _value;
                public Foo(string value) => _value = value;

                public static explicit operator int(Foo foo) => int.Parse(foo._value, System.Globalization.CultureInfo.InvariantCulture);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_IntToObject()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.{|MA0078:Select|}(item => (System.Object)item);
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.Cast<object>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_IntEnumToByte()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.Select(item => (byte)item);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task OptimizeLinq_ByteEnumToByte()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum : System.Byte
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.{|MA0078:Select|}(item => (System.Byte)item);
                }
            }
            """;
        test.FixedCode = """
            using System.Linq;
            using System.Collections.Generic;

            enum TestEnum : System.Byte
            {
                A,
                B,
            }

            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<TestEnum>();
                    source.Cast<byte>();
                }
            }
            """;

        return test.RunAsync();
    }
}
