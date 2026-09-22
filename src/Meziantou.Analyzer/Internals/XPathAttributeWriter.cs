using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Adds the attributes of a node or of an operation to a list, in the namespace of the navigator that owns them.
/// The attributes no query can select are skipped, as computing their value is not free.
/// </summary>
internal readonly struct XPathAttributeWriter(List<XPathAttribute> attributes, string namespaceUri, Dictionary<string, string>? qualifiedNames, XPathAttributeFilter filter)
{
    /// <summary>
    /// Indicates whether the attribute of this name can be selected by a query. The value of an attribute that is
    /// not included does not need to be computed.
    /// </summary>
    public bool Includes(string name) => filter.Includes(name);

    /// <summary>
    /// Indicates whether at least one of the attributes of these names can be selected by a query.
    /// </summary>
    public bool IncludesAny(string[] names) => filter.IncludesAny(names);

    /// <summary>
    /// Adds an attribute, unless it has no value. An attribute that is not present selects nothing, which is how a
    /// query tests whether there is a value at all.
    /// </summary>
    public void Add(TextSpan span, string name, string? value)
    {
        if (value is null or "" || !filter.Includes(name))
            return;

        attributes.Add(new XPathAttribute(qualifiedNames is null ? name : qualifiedNames[name], name, namespaceUri, value, span));
    }
}
