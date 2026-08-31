using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTest = Meziantou.Analyzer.Test.Harness.CSharpAnalyzerTest<
    Meziantou.Analyzer.Rules.DoNotIgnoreReturnValueAnalyzer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class DoNotIgnoreReturnValueAnalyzerTests
{
    private static AnalyzerTest CreateTest()
    {
        var test = new AnalyzerTest();
        test.TestState.AddMeziantouAnnotations();

        // The analyzer declares two descriptors with the same MA0060 id, so the markup cannot tell them apart
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
        return test;
    }

    [Fact]
    public Task Stream_Read_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var stream = File.OpenRead("");
                    {|MA0060:stream.Read(null, 0, 0)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Stream_ReadAsync_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                async void A()
                {
                    var stream = File.OpenRead("");
                    await {|MA0060:stream.ReadAsync(null, 0, 0)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Stream_ReadAsync_ReturnValueUsed_DiscardOperator()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                async void A()
                {
                    var stream = File.OpenRead("");
                    _ = await stream.ReadAsync(null, 0, 0);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Stream_Read_ReturnValueUsed_MethodCall()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var stream = File.OpenRead("");
                    System.Console.Write(stream.Read(null, 0, 0));
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Stream_ReadByte_ReturnValueNotUsed_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var stream = File.OpenRead("");
                    stream.ReadByte();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TextReader_ReadLine_ReturnValueNotUsed_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var reader = new StringReader("test");
                    reader.ReadLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TextReader_ReadLine_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A()
                {
                    var reader = new StringReader("test");
                    var line = reader.ReadLine();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BinaryReader_ReadInt32_ReturnValueNotUsed_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A(BinaryReader reader)
                {
                    reader.ReadInt32();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_ReturnValue_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using Meziantou.Analyzer.Annotations;
            class Test
            {
                [return: DoNotIgnore]
                static int Compute() => 0;

                void A()
                {
                    {|MA0060:Compute()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_ReturnValue_Used()
    {
        var test = CreateTest();
        test.TestCode = """
            using Meziantou.Analyzer.Annotations;
            class Test
            {
                [return: DoNotIgnore]
                static int Compute() => 0;

                void A()
                {
                    var result = Compute();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_ReturnValue_WithMessage()
    {
        var test = CreateTest();
        test.TestCode = """
            using Meziantou.Analyzer.Annotations;
            class Test
            {
                [return: DoNotIgnore(Message = "Use the result to check success")]
                static int Compute() => 0;

                void A()
                {
                    {|MA0060:Compute()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_OutParameter_Discarded()
    {
        var test = CreateTest();
        test.TestCode = """
            using Meziantou.Analyzer.Annotations;
            class Test
            {
                static bool TryGet([DoNotIgnore] out int value) { value = 0; return true; }

                void A()
                {
                    TryGet({|MA0060:out _|});
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Attribute_OutParameter_NotDiscarded()
    {
        var test = CreateTest();
        test.TestCode = """
            using Meziantou.Analyzer.Annotations;
            class Test
            {
                static bool TryGet([DoNotIgnore] out int value) { value = 0; return true; }

                void A()
                {
                    TryGet(out int x);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Pure_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.Contracts;
            class Test
            {
                [Pure]
                static int Compute() => 0;

                void A()
                {
                    {|MA0060:Compute()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Pure_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.Contracts;
            class Test
            {
                [Pure]
                static int Compute() => 0;

                void A()
                {
                    var result = Compute();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Pure_OnClass_NoMethodDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.Contracts;
            [Pure]
            class MyClass
            {
                public int Compute() => 0;
            }
            class Test
            {
                void A(MyClass obj)
                {
                    obj.Compute();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Pure_JetBrainsAttribute_IsAlsoSupported_WhenSystemDiagnosticsContractsPureExists()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.Contracts;

            namespace JetBrains.Annotations
            {
                [System.AttributeUsage(System.AttributeTargets.Method)]
                sealed class PureAttribute : System.Attribute
                {
                }
            }

            class Test
            {
                [JetBrains.Annotations.Pure]
                static int Compute() => 0;

                void A()
                {
                    {|MA0060:Compute()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Trim_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string s)
                {
                    {|MA0060:s.Trim()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Trim_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string s)
                {
                    var trimmed = s.Trim();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Replace_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string s)
                {
                    {|MA0060:s.Replace("a", "b")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Format_ArrowVoidMethod_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A() => {|MA0060:string.Format("{0}", 1)|};
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Format_ArrowStringMethod_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                string A() => string.Format("{0}", 1);
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryParse_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0060:int.TryParse("42", out _)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryParse_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    if (int.TryParse("42", out var value)) { }
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryParse_CustomMethod_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                static bool TryParseItem(string s, out int result) { result = 0; return true; }

                void A()
                {
                    {|MA0060:TryParseItem("42", out _)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryParse_ReturnValueNotUsed_DisabledUsingConfiguration()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0060.enable_tryparse_pattern", "false");
        test.TestCode = """
            class Test
            {
                void A()
                {
                    int.TryParse("42", out _);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryParse_ReturnValueNotUsed_InvalidConfiguration_UsesDefaultValue()
    {
        var test = CreateTest();
        test.TestState.SetConfiguration("MA0060.enable_tryparse_pattern", "invalid");
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0060:int.TryParse("42", out _)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableList_Add_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableList<int> list)
                {
                    {|MA0060:list.Add(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableList_Add_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableList<int> list)
                {
                    var newList = list.Add(1);
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableDictionary_Remove_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableDictionary<string, int> dict)
                {
                    {|MA0060:dict.Remove("key")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableStack_Push_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableStack<int> stack)
                {
                    {|MA0060:stack.Push(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableArray_Create_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A()
                {
                    {|MA0060:ImmutableArray.Create(1, 2, 3)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Stream_ReadAtLeast_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A(Stream stream, byte[] buffer)
                {
                    {|MA0060:stream.ReadAtLeast(buffer, 1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TextReader_Read_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A(TextReader reader)
                {
                    {|MA0060:reader.Read()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task BinaryReader_Read_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.IO;
            class Test
            {
                void A(BinaryReader reader)
                {
                    {|MA0060:reader.Read()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_ToUpper_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A(string s)
                {
                    {|MA0060:s.ToUpper()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task String_Join_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0060:string.Join(", ", "a", "b")|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableDictionary_Add_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableDictionary<string, int> dict)
                {
                    {|MA0060:dict.Add("key", 1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableQueue_Enqueue_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableQueue<int> queue)
                {
                    {|MA0060:queue.Enqueue(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableSet_Add_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableHashSet<int> set)
                {
                    {|MA0060:set.Add(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task ImmutableArrayBuilder_IndexOf_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Immutable;
            class Test
            {
                void A(ImmutableArray<int>.Builder builder)
                {
                    {|MA0060:builder.IndexOf(1)|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HResult_ReturnValueNotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    {|MA0060:NativeMethod()|};
                }

                Windows.Win32.Foundation.HRESULT NativeMethod() => default;
            }

            namespace Windows.Win32.Foundation
            {
                public struct HRESULT { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HResult_ReturnValueUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    var hr = NativeMethod();
                }

                Windows.Win32.Foundation.HRESULT NativeMethod() => default;
            }

            namespace Windows.Win32.Foundation
            {
                public struct HRESULT { }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task HResult_ThrowOnFailure_ReturnValueNotUsed_NoDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Test
            {
                void A()
                {
                    NativeMethod().ThrowOnFailure();
                }

                Windows.Win32.Foundation.HRESULT NativeMethod() => default;
            }

            namespace Windows.Win32.Foundation
            {
                public struct HRESULT
                {
                    public HRESULT ThrowOnFailure() => this;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_SimpleMethod_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample")]
            class Test
            {
                static int Sample() => 42;

                void A()
                {
                    {|MA0060:Sample()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_SimpleMethod_Used()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample")]
            class Test
            {
                static int Sample() => 42;

                void A()
                {
                    var value = Sample();
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_NestedTypeMethod_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Nested.Sample")]
            class Test
            {
                class Nested
                {
                    public static int Sample() => 42;
                }

                void A()
                {
                    {|MA0060:Nested.Sample()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_GenericTypeMethod_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test`1.Sample")]
            class Test<T>
            {
                static int Sample() => 42;

                void A()
                {
                    {|MA0060:Sample()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_GenericMethod_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.Sample``1")]
            class Test
            {
                static int Sample<T>() => 42;

                void A()
                {
                    {|MA0060:Sample<int>()|};
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task AssemblyAttribute_MultipleEntries_NotUsed()
    {
        var test = CreateTest();
        test.TestCode = """
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.SampleA")]
            [assembly: Meziantou.Analyzer.Annotations.DoNotIgnore("M:Test.SampleB")]
            class Test
            {
                static int SampleA() => 1;
                static int SampleB() => 2;

                void A()
                {
                    {|MA0060:SampleA()|};
                    {|MA0060:SampleB()|};
                }
            }
            """;

        return test.RunAsync();
    }
}
