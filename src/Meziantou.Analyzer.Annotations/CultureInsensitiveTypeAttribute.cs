#pragma warning disable CS1591
namespace Meziantou.Analyzer.Annotations;

/// <summary>
/// Indicates that the type is culture insensitive. This can be used to suppress rules such as <c>MA0075</c>, <c>MA0076</c>.
/// <para><code>[CultureInsensitiveType]class Foo { }</code></para>
/// <para><code>[assembly: CultureInsensitiveType(typeof(Foo))]</code></para>
/// </summary>
[System.Diagnostics.Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]
[System.AttributeUsage(System.AttributeTargets.Assembly | System.AttributeTargets.Struct | System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CultureInsensitiveTypeAttribute : System.Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// This can be applied on a given type to mark all its formats as culture insensitive.
    /// </remarks>
    public CultureInsensitiveTypeAttribute() { }
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class with the specified format.
    /// </summary>
    /// <remarks>
    /// This can be applied on a given type to mark the specified format as culture insensitive for that type.
    /// </remarks>
    public CultureInsensitiveTypeAttribute(string? format) => Format = format;
	
	/// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class.
    /// </summary>
    /// <param name="isDefaultFormatCultureInsensitive">When <see langword="true"/>, marks the default format (i.e., the <c>ToString()</c> method) of the type on which it is applied as culture insensitive.</param>
    /// <remarks>
    /// This can be applied on a given type to mark the default format (i.e., the <c>ToString()</c> method) as culture insensitive.
    /// </remarks>
    public CultureInsensitiveTypeAttribute(bool isDefaultFormatCultureInsensitive) => IsDefaultFormatCultureInsensitive = isDefaultFormatCultureInsensitive;

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class with the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which to mark all formats as culture insensitive </param>
    /// <remarks>
    /// This can be applied on an <see cref="System.Reflection.Assembly"/> to mark all formats of the specified <see cref="Type"/> as culture insensitive.
    /// </remarks>
    public CultureInsensitiveTypeAttribute(System.Type type) => Type = type;

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class with the specified <see cref="Type"/> and format.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which to mark the specified format as culture insensitive </param>
    /// <param name="format">The format to mark as culture insensitive </param>
    /// <remarks>
    /// This can be applied on an <see cref="System.Reflection.Assembly"/> to mark the format of the specified <see cref="Type"/> as culture insensitive.
    /// </remarks>
    public CultureInsensitiveTypeAttribute(System.Type type, string? format)
        => (Type, Format) = (type, format);

    /// <summary>
    /// Initializes a new instance of the <see cref="CultureInsensitiveTypeAttribute"/> class.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> for which to mark the <c>ToString()</c> method as culture insensitive </param>
    /// <param name="isDefaultFormatCultureInsensitive">When <see langword="true"/>, marks the default format (i.e., the <c>ToString()</c> method) of <paramref name="type"/> as culture insensitive.</param>
    /// <remarks>
    /// This can be applied on an <see cref="System.Reflection.Assembly"/> to mark the default format (i.e., the <c>ToString()</c> method) of <paramref name="type"/> as culture insensitive.
    /// </remarks>
    public CultureInsensitiveTypeAttribute(System.Type type, bool isDefaultFormatCultureInsensitive)
        => (Type, IsDefaultFormatCultureInsensitive) = (type, isDefaultFormatCultureInsensitive);

    /// <summary>
    /// Gets the <see cref="Type"/> for which to change the culture insensitivity.
    /// </summary>
    /// <value>
    /// The <see cref="Type"/> for which to change the culture insensitivity, or <see langword="null"/> to change the culture insensitivity of
	/// the type on which the attribute is applied.
    /// </value>
    public Type? Type { get; }
	
    /// <summary>
    /// Gets the format for which to change the culture insensitivity.
    /// </summary>
    /// <value>
    /// The format for which to change the culture insensitivity.
    /// </value>
    public string? Format { get; }
	
    /// <summary>
    /// Gets a value indicating whether the default format (i.e., the <c>ToString()</c> method) is culture insensitive.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to mark the default format (i.e., the <c>ToString()</c> method) as culture insensitive;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDefaultFormatCultureInsensitive { get; }
}
