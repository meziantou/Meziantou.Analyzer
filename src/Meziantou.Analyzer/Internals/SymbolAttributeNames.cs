namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The names of the attributes that expose a symbol that is not a type, such as <c>TargetMethod</c> for the method
/// an invocation calls.
/// </summary>
internal sealed record SymbolAttributeNames(string QualifiedName, string Name, string DocumentationId, string Kind, string IsStatic);
