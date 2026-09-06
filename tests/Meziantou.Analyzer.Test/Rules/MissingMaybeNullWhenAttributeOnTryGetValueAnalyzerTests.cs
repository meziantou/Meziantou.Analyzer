using CodeFixTest = Meziantou.Analyzer.Test.Harness.CSharpCodeFixTest<
    Meziantou.Analyzer.Rules.MissingMaybeNullWhenAttributeOnTryGetValueAnalyzer,
    Meziantou.Analyzer.Rules.MissingMaybeNullWhenAttributeOnTryGetValueFixer>;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MissingMaybeNullWhenAttributeOnTryGetValueAnalyzerTests
{
    [Fact]
    public Task TryGetValue_IDictionary_ExplicitWithoutAttribute_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>
            {
                bool IDictionary<string, string?>.TryGetValue(string key, out string? {|MA0221:value|})
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
        var test = new CodeFixTest();
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
    public Task TryGetValue_IDictionary_WithoutAttribute_ShouldReportDiagnostic()
    {
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0221:value|})
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
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0221:value|})
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;

            class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0221:stringValue|})
                {
                    stringValue = null;
                    return false;
                }

                public bool TryGetValue(int key, out string? {|MA0221:intValue|})
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
        var test = new CodeFixTest();
        test.TestCode = """
            using System.Collections;
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
            {
                public bool TryGetValue(string key, out string? {|MA0221:stringValue|})
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
        var test = new CodeFixTest();
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
