using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MissingNotNullWhenAttributeOnEqualsAnalyzer,
    Meziantou.Analyzer.Rules.MissingNotNullWhenAttributeOnEqualsFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzerTests
{
    // The analyzer declares two descriptors with the same MA0186 id, so the markup cannot tell them apart
    private static CodeFixTest CreateTest()
    {
        var test = new CodeFixTest();
        test.MarkupOptions = MarkupOptions.UseFirstDescriptor;
        return test;
    }

    [Fact]
    public Task Equals_Object_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithoutAttribute_ShouldFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;
        test.FixedCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithWrongAttributeValue_ShouldFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(false)] object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;
        test.FixedCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_ExplicitWithoutAttribute_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>
            {
                bool IDictionary<string, string?>.TryGetValue(string key, out string? {|MA0186:value|})
                {
                    value = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_ExplicitWithAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>
            {
                bool IDictionary<string, string?>.TryGetValue(string key, [MaybeNullWhen(false)] out string? value)
                {
                    value = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_Object_WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Diagnostics.CodeAnalysis;

            class Sample
            {
                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Sample : IEquatable<Sample>
            {
                public bool Equals(Sample? {|MA0186:other|})
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;
            using System.Diagnostics.CodeAnalysis;

            class Sample : IEquatable<Sample>
            {
                public bool Equals([NotNullWhen(true)] Sample? other)
                {
                    return false;
                }

                public override bool Equals([NotNullWhen(true)] object? obj)
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NonNullableParameter_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task NotEqualsMethod_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public bool IsEqual(object? obj)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task PrivateEquals_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                private bool Equals(object? obj)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task StaticEquals_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public static bool Equals(object? obj1, object? obj2)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_WrongSignature_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class Sample
            {
                public bool Equals(object? obj, int x)
                {
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_BothMethodsWithoutAttribute_ShouldReportBothDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            class Sample : IEquatable<Sample>
            {
                public bool Equals(Sample? {|MA0186:other|})
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return Equals(obj as Sample);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_IEquatable_ValueType_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System;

            struct Sample : IEquatable<Sample>
            {
                public bool Equals(Sample other)
                {
                    return false;
                }

                public override bool Equals(object? {|MA0186:obj|})
                {
                    return obj is Sample other && Equals(other);
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableDisabled_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            #nullable disable
            class Sample
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableEnabled_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            #nullable enable
            class Sample
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task Equals_NullableEnabledThenDisabled_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            #nullable enable
            class Sample1
            {
                public override bool Equals(object? {|MA0186:obj|})
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }

            #nullable disable
            class Sample2
            {
                public override bool Equals(object obj)
                {
                    return false;
                }

                public override int GetHashCode() => 0;
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0186:value|})
                {
                    value = null;
                    return false;
                }

                // Other IDictionary members...
                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_WithoutAttribute_ShouldFix()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0186:value|})
                {
                    value = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;
        test.FixedCode = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, [MaybeNullWhen(false)] out string? value)
                {
                    value = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_WithAttribute_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, [MaybeNullWhen(false)] out string? value)
                {
                    value = null;
                    return false;
                }

                // Other IDictionary members...
                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string?> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_Twice_BothInvalid_ShouldReportDiagnostics()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0186:stringValue|})
                {
                    stringValue = null;
                    return false;
                }

                public bool TryGetValue(int key, out string? {|MA0186:intValue|})
                {
                    intValue = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public string? this[int key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

                ICollection<string> IDictionary<string, string?>.Keys => throw new System.NotImplementedException();
                ICollection<int> IDictionary<int, string?>.Keys => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<string, string?>.Values => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<int, string?>.Values => throw new System.NotImplementedException();

                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(int key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public bool ContainsKey(int key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<int, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(int key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<int, string?>> IEnumerable<KeyValuePair<int, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_Twice_OneInvalid_ShouldReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0186:stringValue|})
                {
                    stringValue = null;
                    return false;
                }

                public bool TryGetValue(int key, [MaybeNullWhen(false)] out string? intValue)
                {
                    intValue = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public string? this[int key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

                ICollection<string> IDictionary<string, string?>.Keys => throw new System.NotImplementedException();
                ICollection<int> IDictionary<int, string?>.Keys => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<string, string?>.Values => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<int, string?>.Values => throw new System.NotImplementedException();

                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(int key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public bool ContainsKey(int key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<int, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(int key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<int, string?>> IEnumerable<KeyValuePair<int, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_IDictionary_Twice_NoneInvalid_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
            {
                public bool TryGetValue(string key, [MaybeNullWhen(false)] out string? stringValue)
                {
                    stringValue = null;
                    return false;
                }

                public bool TryGetValue(int key, [MaybeNullWhen(false)] out string? intValue)
                {
                    intValue = null;
                    return false;
                }

                public string? this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public string? this[int key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

                ICollection<string> IDictionary<string, string?>.Keys => throw new System.NotImplementedException();
                ICollection<int> IDictionary<int, string?>.Keys => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<string, string?>.Values => throw new System.NotImplementedException();
                ICollection<string?> IDictionary<int, string?>.Values => throw new System.NotImplementedException();

                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string? value) => throw new System.NotImplementedException();
                public void Add(int key, string? value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public bool ContainsKey(int key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<int, string?>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(int key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string?> item) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<int, string?> item) => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<string, string?>> IEnumerable<KeyValuePair<string, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator<KeyValuePair<int, string?>> IEnumerable<KeyValuePair<int, string?>>.GetEnumerator() => throw new System.NotImplementedException();
                IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_NotIDictionary_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            class MyClass
            {
                public bool TryGetValue(string key, out string? value)
                {
                    value = null;
                    return false;
                }
            }
            """;

        return test.RunAsync();
    }

    [Fact]
    public Task TryGetValue_NonNullableValue_ShouldNotReportDiagnostic()
    {
        var test = CreateTest();
        test.TestCode = """
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string>
            {
                public bool TryGetValue(string key, out string value)
                {
                    value = "";
                    return false;
                }

                // Other IDictionary members...
                public string this[string key] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
                public ICollection<string> Keys => throw new System.NotImplementedException();
                public ICollection<string> Values => throw new System.NotImplementedException();
                public int Count => throw new System.NotImplementedException();
                public bool IsReadOnly => throw new System.NotImplementedException();
                public void Add(string key, string value) => throw new System.NotImplementedException();
                public void Add(KeyValuePair<string, string> item) => throw new System.NotImplementedException();
                public void Clear() => throw new System.NotImplementedException();
                public bool Contains(KeyValuePair<string, string> item) => throw new System.NotImplementedException();
                public bool ContainsKey(string key) => throw new System.NotImplementedException();
                public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new System.NotImplementedException();
                public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new System.NotImplementedException();
                public bool Remove(string key) => throw new System.NotImplementedException();
                public bool Remove(KeyValuePair<string, string> item) => throw new System.NotImplementedException();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
            }
            """;

        return test.RunAsync();
    }
}
