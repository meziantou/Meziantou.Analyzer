using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Adds the attributes of a node or of an operation to a list, in the namespace of the navigator that owns them.
/// </summary>
internal readonly struct XPathAttributeWriter(List<XPathAttribute> attributes, string namespaceUri, Dictionary<string, string>? qualifiedNames)
{
    /// <summary>
    /// Adds an attribute, unless it has no value. An attribute that is not present selects nothing, which is how a
    /// query tests whether there is a value at all.
    /// </summary>
    public void Add(TextSpan span, string name, string? value)
    {
        if (value is null or "")
            return;

        attributes.Add(new XPathAttribute(qualifiedNames is null ? name : qualifiedNames[name], name, namespaceUri, value, span));
    }
}
