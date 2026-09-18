using Microsoft.CodeAnalysis.CSharp;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.UseArrayEmptyAnalyzer,
    Meziantou.Analyzer.Rules.UseArrayEmptyFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class UseArrayEmptyAnalyzerTests
{
    private static CodeFixTest CreateTest() => new();

    [Theory]
    [InlineData("new int[0]")]
    [InlineData("new int[0L]")]
    [InlineData("new int[0u]")]
    [InlineData("new int[] { }")]
    public Task EmptyArray_ShouldReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var a = {|MA0005:{{code}}|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    var a = System.Array.Empty<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new int[1]")]
    [InlineData("new int[1L]")]
    [InlineData("new int[1u]")]
    [InlineData("new int[0, 0]")]
    [InlineData("new int[] { 0 }")]
    public Task NonEmptyArray_ShouldNotReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            class TestClass
            {
                void Test()
                {
                    var a = {{code}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Theory]
    [InlineData("new int*[0]")]
    [InlineData("new int*[] { }")]
    [InlineData("new delegate*<void>[0]")]
    public Task EmptyPointerArray_ShouldNotReportError(string code)
    {
        var test = CreateTest();
        test.TestCode = $$"""
            unsafe class TestClass
            {
                void Test()
                {
                    var a = {{code}};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyArrayOfPointerArrays_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            unsafe class TestClass
            {
                void Test()
                {
                    var a = {|MA0005:new int*[0][]|};
                }
            }
            """;
        test.FixedCode = """
            unsafe class TestClass
            {
                void Test()
                {
                    var a = System.Array.Empty<int*[]>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Length_FlowedFromLocal_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int length = 0;
                    var a = {|MA0005:new int[length]|};
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    int length = 0;
                    var a = System.Array.Empty<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsMethod_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            public class TestClass
            {
                public void Test(params string[] values)
                {
                }

                public void CallTest()
                {
                    Test();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsConstructor_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            public class TestClass
            {
                public TestClass(params string[] values)
                {
                }

                public static TestClass Create() => new TestClass();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ParamsIndexer_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            public class TestClass
            {
                public int this[int index, params string[] values] => 0;

                public int Get() => this[0];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task EmptyArrayInAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [Test(new int[0])]
            class TestAttribute : System.Attribute
            {
                public TestAttribute(int[] data) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImplicitEmptyArrayInAttribute_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            [Test("test")]
            class TestAttribute : System.Attribute
            {
                public TestAttribute(string a, params object[] data) { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task CollectionExpression_ParamsConstructor_ShouldNotReportError()
    {
        var test = CreateTest();
        test.LanguageVersion = LanguageVersion.CSharp12;
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            public class TheoryData<T> : IEnumerable<T>
            {
                public TheoryData(params T[] values) { }
                public void Add(T value) { }
                public IEnumerator<T> GetEnumerator() => throw null;
                IEnumerator IEnumerable.GetEnumerator() => throw null;
            }

            public class TestClass
            {
                public static TheoryData<string> Data => ["foo", "bar"];
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ArrayInitializer_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test()
                {
                    int[] a = [|{ }|];
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test()
                {
                    int[] a = System.Array.Empty<int>();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task FieldArrayInitializer_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int[] a = [|{ }|];
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                int[] a = System.Array.Empty<int>();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ExplicitEmptyArrayPassedToParams_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                void Test(params string[] values)
                {
                }

                void CallTest()
                {
                    Test([|new string[0]|]);
                }
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                void Test(params string[] values)
                {
                }

                void CallTest()
                {
                    Test(System.Array.Empty<string>());
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task GenericElementType_ShouldReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                T[] Test<T>() => [|new T[0]|];
            }
            """;
        test.FixedCode = """
            class TestClass
            {
                T[] Test<T>() => System.Array.Empty<T>();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NonConstantLength_ShouldNotReportError()
    {
        var test = CreateTest();
        test.TestCode = """
            class TestClass
            {
                int[] Test(int length) => new int[length];
            }
            """;

        return test.RunAsync();
    }
}
