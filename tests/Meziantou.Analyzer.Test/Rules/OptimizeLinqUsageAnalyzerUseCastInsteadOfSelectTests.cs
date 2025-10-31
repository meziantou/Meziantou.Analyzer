using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class OptimizeLinqUsageAnalyzerUseCastInsteadOfSelectTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<OptimizeLinqUsageAnalyzer>(id: RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect)
            .WithCodeFixProvider<OptimizeLinqUsageFixer>();
    }

    [Theory]
    [InlineData("source.[|Select|](dt => (BaseType)dt)",
                "source.Cast<BaseType>()")]
    [InlineData("Enumerable.[|Select|](source, dt => (Test.BaseType)dt).FirstOrDefault()",
                "source.Cast<BaseType>().FirstOrDefault()")]
    [InlineData("System.Linq.Enumerable.Empty<DerivedType>().[|Select|](dt => (Gen.IList<string>)dt)",
                            "Enumerable.Empty<DerivedType>().Cast<Gen.IList<string>>()")]
    [InlineData("Enumerable.Range(0, 1).[|Select<int, object>|](i => i)",
                "Enumerable.Range(0, 1).Cast<object>()")]
    [InlineData("source.[|Select|](i => (object?)i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.[|Select|](i => (object)i)",
                "source.Cast<object>()",
                true)]
    [InlineData("source.[|Select<DerivedType, object?>|](i => i)",
                "source.Cast<object?>()",
                true)]
    [InlineData("source.[|Select<DerivedType, object>|](i => i)",
                "source.Cast<object>()",
                true)]
    public async Task OptimizeLinq_WhenSelectorReturnsCastElement_ReplacesSelectByCast(
        string selectInvocation,
        string expectedReplacement,
        bool enableNullable = false)
    {
        var originalCode = $$"""
            #nullable {(enableNullable ? "enable" : "disable")}
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
        var modifiedCode = $$"""
            #nullable {(enableNullable ? "enable" : "disable")}
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(modifiedCode)
              .ValidateAsync();
    }

    [Theory]
    [InlineData("source.Select(dt => dt.Name)")]            // No cast
    [InlineData("source.Select(dt => (object)dt.Name)")]    // Cast of property, not of element itself
    [InlineData("source.Select(dt => dt as BaseType)")]     // 'as' operator should not be replaced by Cast<>
    public async Task OptimizeLinq_WhenSelectorDoesNotReturnCastElement_NoDiagnosticReported(string selectInvocation)
    {
        var originalCode = $$"""
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptimizeLinq_ExplicitCastIsRequired()
    {
        var originalCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public async Task OptimizeLinq_UserDefinedImplicitOperator()
    {
        var originalCode = """
            using System;
            using System.Linq;
            
            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo(""1""), new Foo(""42"") };
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    [Trait("IssueId", "https://github.com/meziantou/Meziantou.Analyzer/issues/176")]
    public async Task OptimizeLinq_UserDefinedImplicitOperator_ImplicitUse()
    {
        var originalCode = """
            using System;
            using System.Linq;
            
            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo(""1""), new Foo(""42"") };
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptimizeLinq_UserDefinedExplicitOperator()
    {
        var originalCode = """
            using System;
            using System.Linq;
            
            static class P
            {
                static void Main()
                {
                    var foos = new[] { new Foo(""1""), new Foo(""42"") };
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptimizeLinq_IntToObject()
    {
        var originalCode = """
            using System.Linq;
            using System.Collections.Generic;
            
            class Test
            {
                public Test()
                {
                    var source = System.Linq.Enumerable.Empty<int>();
                    source.[|Select|](item => (System.Object)item);
                }
            }
            """;
        var fixedCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(fixedCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptimizeLinq_IntEnumToByte()
    {
        var originalCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ValidateAsync();
    }

    [Fact]
    public async Task OptimizeLinq_ByteEnumToByte()
    {
        var originalCode = """
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
                    source.[|Select|](item => (System.Byte)item);
                }
            }
            """;
        var fixedCode = """
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
        await CreateProjectBuilder()
              .WithSourceCode(originalCode)
              .ShouldFixCodeWith(fixedCode)
              .ValidateAsync();
    }
}
