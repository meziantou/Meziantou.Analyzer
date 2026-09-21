using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// An attribute of an element of the documents exposed to the XPath queries. <see cref="Name"/> is prefixed when the
/// attribute is in a namespace, whereas <see cref="LocalName"/> never is.
/// </summary>
internal readonly record struct XPathAttribute(string Name, string LocalName, string NamespaceUri, string Value, TextSpan Span);
