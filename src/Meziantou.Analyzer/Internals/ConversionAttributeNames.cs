namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The names of the attributes that expose a <see cref="Microsoft.CodeAnalysis.Operations.CommonConversion"/>, such
/// as <c>ConversionIsUserDefined</c> for the conversion of an <c>operation:Conversion</c>. The members that are not
/// in every supported version of Roslyn are not exposed, so an entry means the same thing in all of them.
/// </summary>
internal sealed record ConversionAttributeNames(string Exists, string IsIdentity, string IsImplicit, string IsNullable, string IsNumeric, string IsReference, string IsUserDefined, SymbolAttributeNames MethodNames)
{
    public string[] All { get; } = [Exists, IsIdentity, IsImplicit, IsNullable, IsNumeric, IsReference, IsUserDefined, .. MethodNames.All];
}
