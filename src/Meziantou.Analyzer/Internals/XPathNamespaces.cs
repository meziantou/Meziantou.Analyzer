namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The namespaces of the XPath queries of the banned syntax files. They are defined by
/// <see cref="BannedSyntaxXsltContext"/>, which compiles the queries, as the prefixes are resolved before the
/// document a query is evaluated on is known.
/// </summary>
internal static class XPathNamespaces
{
    /// <summary>The prefix of the attributes that expose the data of the semantic model.</summary>
    public const string SemanticPrefix = "semantic";

    /// <summary>The prefix of the elements that expose the operations.</summary>
    public const string OperationPrefix = "operation";

    /// <summary>The prefix of the elements that expose the symbols.</summary>
    public const string SymbolPrefix = "symbol";

    public const string SemanticNamespaceUri = "urn:meziantou.analyzer:semantic";
    public const string OperationNamespaceUri = "urn:meziantou.analyzer:operation";
    public const string SymbolNamespaceUri = "urn:meziantou.analyzer:symbol";
}
