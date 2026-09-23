namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Matches a member against the name a query gives to the <c>overrides</c> and <c>implements-member</c> functions.
/// The name is either the documentation comment id of the member, such as <c>M:System.Object.Equals(System.Object)</c>,
/// which selects a single overload, or its name qualified by the metadata name of its containing type, such as
/// <c>System.Object.Equals</c>, which selects all the overloads. A qualified name cannot contain ':', so the format
/// is detected from the name.
/// </summary>
internal sealed class XPathMemberNameMatcher
{
    private readonly string _name;
    private readonly bool _isDocumentationId;

    public XPathMemberNameMatcher(string name)
    {
        _name = name;
        _isDocumentationId = name.Length >= 2 && char.IsLetter(name[0]) && name[1] is ':';
    }

    public bool Matches(ISymbol member)
    {
        // The name of the member is part of both formats, so it is tested before formatting the full name, as most
        // of the members a query visits do not match. The documentation comment id uses the metadata name, which is
        // 'Item' for an indexer whose name is 'this[]'.
        if (!_name.Contains(_isDocumentationId ? member.MetadataName : member.Name, StringComparison.Ordinal))
            return false;

        var name = _isDocumentationId ? SymbolNameFormatter.GetDocumentationId(member) : SymbolNameFormatter.GetSymbolName(member);
        return string.Equals(name, _name, StringComparison.Ordinal);
    }
}
