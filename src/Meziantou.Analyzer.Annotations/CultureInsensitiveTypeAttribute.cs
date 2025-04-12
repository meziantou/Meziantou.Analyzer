#pragma warning disable CS1591

using System;

namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the type is culture insensitive. This can be used to suppress rules such as <c>MA0075</c>, <c>MA0076</c> or <c>MA0107</c>.
/// <para><code>[CultureInsensitiveType]class Foo { }</code></para>
/// <para><code>[assembly: CultureInsensitiveType(typeof(Foo))]</code></para>
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Assembly | System.AttributeTargets.Struct | System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CultureInsensitiveTypeAttribute : System.Attribute
{
    public CultureInsensitiveTypeAttribute() { }
    public CultureInsensitiveTypeAttribute(System.Type type) => Type = type;

    public Type? Type { get; }
}
