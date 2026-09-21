using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The operations of a file, with the data the navigators compute from them. It is built once per file and shared by
/// the navigator and all its clones, as an XPath evaluation clones the navigator for every step.
/// </summary>
internal sealed class OperationForest
{
    /// <summary>A forest with no operation, which is used to validate a query without a compilation.</summary>
    public static readonly OperationForest Empty = new([]);

    private static readonly IOperation[] NoChildren = [];

    // The operations of Roslyn do not override Equals and GetHashCode, so the default comparer is reference equality.
    // The forest is used by a single analysis of a single file, which is not concurrent.
    private readonly Dictionary<IOperation, IOperation[]> _children = [];
    private readonly Dictionary<IOperation, XPathAttribute[]> _attributes = [];

    private OperationForest(IOperation[] roots) => Roots = roots;

    /// <summary>
    /// The operations that have no parent, in document order. They are the bodies of the members, the initializers
    /// of the fields, of the properties and of the parameters, and the attributes.
    /// </summary>
    public IOperation[] Roots { get; }

    public static OperationForest Create(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var roots = new List<IOperation>();
        var lastRootEnd = -1;
        foreach (var node in root.DescendantNodesAndSelf())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The nodes of an operation that was already found are part of its tree
            if (node.SpanStart < lastRootEnd)
                continue;

            if (semanticModel.GetOperation(node, cancellationToken) is { Parent: null } operation)
            {
                roots.Add(operation);
                lastRootEnd = operation.Syntax.Span.End;
            }
        }

        return new OperationForest([.. roots]);
    }

    /// <summary>
    /// The children of an operation. <see cref="IOperation.OperationList"/> cannot be indexed, so they are
    /// materialized to navigate from an operation to its siblings.
    /// </summary>
    public IOperation[] GetChildren(IOperation operation)
    {
        if (_children.TryGetValue(operation, out var children))
            return children;

        var list = operation.GetChildOperations();
        children = list.Count is 0 ? NoChildren : [.. list];
        _children.Add(operation, children);
        return children;
    }

    /// <summary>
    /// The attributes of an operation. They are computed once per file, as a query that tests an attribute visits
    /// the same operation several times.
    /// </summary>
    public XPathAttribute[] GetAttributes(IOperation operation)
    {
        if (_attributes.TryGetValue(operation, out var attributes))
            return attributes;

        attributes = OperationXPathNavigator.BuildAttributes(operation);
        _attributes.Add(operation, attributes);
        return attributes;
    }
}
