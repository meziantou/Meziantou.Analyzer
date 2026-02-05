using Meziantou.Analyzer.Rules;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules;

public sealed class MissingNotNullWhenAttributeOnEqualsAnalyzerTests
{
    private static ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder()
            .WithAnalyzer<MissingNotNullWhenAttributeOnEqualsAnalyzer>();
    }

    [Fact]
    public async Task Equals_Object_WithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public override bool Equals(object? [|obj|])
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_ExplicitWithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;

                class MyDictionary : IDictionary<string, string?>
                {
                    bool IDictionary<string, string?>.TryGetValue(string key, out string? [|value|])
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_ExplicitWithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_Object_WithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Diagnostics.CodeAnalysis;

                class Sample
                {
                    public override bool Equals([NotNullWhen(true)] object? obj)
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_WithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Sample : IEquatable<Sample>
                {
                    public bool Equals(Sample? [|other|])
                    {
                        return false;
                    }

                    public override bool Equals(object? [|obj|])
                    {
                        return Equals(obj as Sample);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_WithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_NonNullableParameter_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public override bool Equals(object obj)
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task NotEqualsMethod_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public bool IsEqual(object? obj)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task PrivateEquals_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    private bool Equals(object? obj)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task StaticEquals_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public static bool Equals(object? obj1, object? obj2)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_WrongSignature_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class Sample
                {
                    public bool Equals(object? obj, int x)
                    {
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_BothMethodsWithoutAttribute_ShouldReportBothDiagnostics()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                class Sample : IEquatable<Sample>
                {
                    public bool Equals(Sample? [|other|])
                    {
                        return false;
                    }

                    public override bool Equals(object? [|obj|])
                    {
                        return Equals(obj as Sample);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_IEquatable_ValueType_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System;

                struct Sample : IEquatable<Sample>
                {
                    public bool Equals(Sample other)
                    {
                        return false;
                    }

                    public override bool Equals(object? [|obj|])
                    {
                        return obj is Sample other && Equals(other);
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_NullableDisabled_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable disable
                class Sample
                {
                    public override bool Equals(object obj)
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_NullableEnabled_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample
                {
                    public override bool Equals(object? [|obj|])
                    {
                        return false;
                    }

                    public override int GetHashCode() => 0;
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task Equals_NullableEnabledThenDisabled_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                #nullable enable
                class Sample1
                {
                    public override bool Equals(object? [|obj|])
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_WithoutAttribute_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections.Generic;

                class MyDictionary : IDictionary<string, string?>
                {
                    public bool TryGetValue(string key, out string? [|value|])
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_WithAttribute_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_Twice_BothInvalid_ShouldReportDiagnostics()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections;
                using System.Collections.Generic;

                class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
                {
                    public bool TryGetValue(string key, out string? [|stringValue|])
                    {
                        stringValue = null;
                        return false;
                    }

                    public bool TryGetValue(int key, out string? [|intValue|])
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_Twice_OneInvalid_ShouldReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                using System.Collections;
                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;

                class MyDictionary : IDictionary<string, string?>, IDictionary<int, string?>
                {
                    public bool TryGetValue(string key, out string? [|stringValue|])
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_IDictionary_Twice_NoneInvalid_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_NotIDictionary_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
                class MyClass
                {
                    public bool TryGetValue(string key, out string? value)
                    {
                        value = null;
                        return false;
                    }
                }
                """)
            .ValidateAsync();
    }

    [Fact]
    public async Task TryGetValue_NonNullableValue_ShouldNotReportDiagnostic()
    {
        await CreateProjectBuilder()
            .WithSourceCode("""
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
                """)
            .ValidateAsync();
    }
}
