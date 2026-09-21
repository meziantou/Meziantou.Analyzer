namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The attributes the queries of a file actually use. Computing an attribute costs a call to the semantic model or a
/// reflection call, so the attributes no query can select are not computed at all. A query that selects the attributes
/// it does not name, such as <c>@*</c> or <c>attribute::</c>, needs all of them.
/// </summary>
internal sealed class XPathAttributeFilter
{
    /// <summary>A filter that includes every attribute.</summary>
    public static readonly XPathAttributeFilter All = new(names: null);

    // null when every attribute is included
    private readonly HashSet<string>? _names;

    private XPathAttributeFilter(HashSet<string>? names) => _names = names;

    /// <summary>
    /// A filter that includes the local names of <paramref name="names"/>, or every attribute when it is
    /// <see langword="null"/>.
    /// </summary>
    public static XPathAttributeFilter Create(HashSet<string>? names) => names is null ? All : new XPathAttributeFilter(names);

    /// <summary>
    /// Indicates whether the attribute of this local name can be selected by a query.
    /// </summary>
    public bool Includes(string name) => _names is null || _names.Contains(name);

    /// <summary>
    /// Indicates whether at least one of the attributes of these local names can be selected by a query.
    /// </summary>
    public bool IncludesAny(string[] names)
    {
        if (_names is null)
            return true;

        foreach (var name in names)
        {
            if (_names.Contains(name))
                return true;
        }

        return false;
    }
}
