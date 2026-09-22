namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The names of the attributes that expose a symbol that is not a type, such as <c>TargetMethod</c> for the method
/// an invocation calls.
/// </summary>
internal sealed record SymbolAttributeNames(string QualifiedName, string Name, string DocumentationId, string Kind, string IsStatic, string IsAbstract, string IsVirtual, string IsOverride, string IsSealed, string IsAsync, string IsExtensionMethod, string Arity)
{
    public string[] All { get; } = [QualifiedName, Name, DocumentationId, Kind, IsStatic, IsAbstract, IsVirtual, IsOverride, IsSealed, IsAsync, IsExtensionMethod, Arity];

    /// <summary>
    /// The names of the attributes a property that returns several symbols exposes. The values of the other
    /// attributes cannot be joined, as a boolean or the name of a member of an enumeration is not meaningful for a list.
    /// </summary>
    public string[] List { get; } = [QualifiedName, Name, DocumentationId];
}
