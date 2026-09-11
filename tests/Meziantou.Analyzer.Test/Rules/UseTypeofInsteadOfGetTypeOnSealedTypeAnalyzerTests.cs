using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseTypeofInsteadOfGetTypeOnSealedTypeAnalyzer,
    Meziantou.Analyzer.Rules.UseTypeofInsteadOfGetTypeOnSealedTypeFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseTypeofInsteadOfGetTypeOnSealedTypeAnalyzerTests
{
    [Theory]
    [InlineData("object value")]
    [InlineData("System.Exception value")]
    [InlineData("System.IDisposable value")]
    [InlineData("System.Collections.Generic.List<int> value")]
    // Arrays are covariant, so the runtime type of an 'object[]' can be a 'string[]'
    [InlineData("string[] value")]
    // Boxing a 'Nullable<T>' creates a 'T', or a null reference when there is no value
    [InlineData("int? value")]
    public Task NonSealedType_NoDiagnostic(string parameter)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                class Sample
                {
                    System.Type Test({{parameter}}) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task TypeParameter_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test<T>(T value) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task AnonymousType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test()
                    {
                        var value = new { Name = "" };
                        return value.GetType();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task SealedGenericTypeOfAnonymousType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Container<T> { }

                static class Container
                {
                    public static Container<T> Create<T>(T value) => new Container<T>();
                }

                class Sample
                {
                    System.Type Test()
                    {
                        var value = Container.Create(new { Name = "" });
                        return value.GetType();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task SealedTypeNestedInAGenericTypeOfAnonymousType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Container<T>
                {
                    public sealed class Item { }

                    public Item GetItem() => new Item();
                }

                static class Container
                {
                    public static Container<T> Create<T>(T value) => new Container<T>();
                }

                class Sample
                {
                    System.Type Test()
                    {
                        var value = Container.Create(new { Name = "" }).GetItem();
                        return value.GetType();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task TupleOfAnonymousType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test()
                    {
                        var value = (new { Name = "" }, 0);
                        return value.GetType();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ArrayOfAnonymousType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test()
                    {
                        var value = new[] { new { Name = "" } };
                        return value.GetType();
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task UnknownType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test({|CS0246:Unknown|} value) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Dynamic_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test(dynamic value) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ConditionalAccess_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test(string value) => value?.GetType();
                }
                """,
        }.RunAsync();
    }

    [Theory]
    // The invocation must be evaluated, as it can have side effects
    [InlineData("Get().GetType()")]
    // The indexer can throw
    [InlineData("values[0].GetType()")]
    // The cast can throw
    [InlineData("((string)value).GetType()")]
    public Task InstanceWithSideEffects_NoDiagnostic(string expression)
    {
        return new CodeFixTest
        {
            TestCode = $$"""
                class Sample
                {
                    System.Type Test(object value, System.Collections.Generic.List<string> values) => {{expression}};

                    static string Get() => "";
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GetTypeOnTypeInstance_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test(System.Type value) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task HiddenGetType_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Sample
                {
                    public new System.Type GetType() => null;

                    System.Type Test(Sample value) => value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task BaseAccess_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Base { }

                sealed class Sample : Base
                {
                    System.Type Test() => base.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Parameter()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test(string value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                class Sample
                {
                    System.Type Test(string value) => typeof(string);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Local()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test()
                    {
                        var value = 0;
                        return [|value.GetType()|];
                    }
                }
                """,
            FixedCode = """
                class Sample
                {
                    System.Type Test()
                    {
                        var value = 0;
                        return typeof(int);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task This()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Sample
                {
                    System.Type Test() => [|GetType()|];
                }
                """,
            FixedCode = """
                sealed class Sample
                {
                    System.Type Test() => typeof(Sample);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Field()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Sample
                {
                    private readonly Sample _value;

                    System.Type Test() => [|_value.GetType()|];
                }
                """,
            FixedCode = """
                sealed class Sample
                {
                    private readonly Sample _value;

                    System.Type Test() => typeof(Sample);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task PropertyGetterModifyingState_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    static int calls;
                    static string Value { get { calls++; return ""; } }

                    System.Type Test() => Value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task PropertyGetterThrowing_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    string Value => throw new System.InvalidOperationException();

                    System.Type Test() => Value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task AutoProperty_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    string Value { get; set; }

                    System.Type Test() => Value.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task FieldOfProperty_NoDiagnostic()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Item
                {
                    public string Field;
                }

                class Sample
                {
                    Item Value => throw new System.InvalidOperationException();

                    System.Type Test() => Value.Field.GetType();
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Enum()
    {
        return new CodeFixTest
        {
            TestCode = """
                enum Color { Red }

                class Sample
                {
                    System.Type Test(Color value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                enum Color { Red }

                class Sample
                {
                    System.Type Test(Color value) => typeof(Color);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Struct()
    {
        return new CodeFixTest
        {
            TestCode = """
                struct Point { }

                class Sample
                {
                    System.Type Test(Point value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                struct Point { }

                class Sample
                {
                    System.Type Test(Point value) => typeof(Point);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GenericSealedType()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test<T>(Container<T> value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test<T>(Container<T> value) => typeof(Container<T>);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task ThisInStruct()
    {
        return new CodeFixTest
        {
            TestCode = """
                struct Point
                {
                    System.Type Test() => [|GetType()|];
                }
                """,
            FixedCode = """
                struct Point
                {
                    System.Type Test() => typeof(Point);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task Record()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed record Person(string Name);

                class Sample
                {
                    System.Type Test(Person value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                sealed record Person(string Name);

                class Sample
                {
                    System.Type Test(Person value) => typeof(Person);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NamedTuple()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test((int Id, string Name) value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                class Sample
                {
                    System.Type Test((int Id, string Name) value) => typeof((int, string));
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task GenericSealedTypeOfDynamic()
    {
        return new CodeFixTest
        {
            TestCode = """
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test(Container<dynamic> value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test(Container<dynamic> value) => typeof(Container<dynamic>);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task NullableReferenceType()
    {
        return new CodeFixTest
        {
            TestCode = """
                #nullable enable
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test()
                    {
                        string? value = "";
                        return [|value.GetType()|];
                    }

                    System.Type Test(Container<string?> value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                #nullable enable
                sealed class Container<T> { }

                class Sample
                {
                    System.Type Test()
                    {
                        string? value = "";
                        return typeof(string);
                    }

                    System.Type Test(Container<string?> value) => typeof(Container<string?>);
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public Task TypeInNamespace()
    {
        return new CodeFixTest
        {
            TestCode = """
                class Sample
                {
                    System.Type Test(System.Text.StringBuilder value) => [|value.GetType()|];
                }
                """,
            FixedCode = """
                using System.Text;

                class Sample
                {
                    System.Type Test(System.Text.StringBuilder value) => typeof(StringBuilder);
                }
                """,
        }.RunAsync();
    }
}
