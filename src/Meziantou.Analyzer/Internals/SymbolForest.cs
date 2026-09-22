using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The symbols declared in a file, organized as a tree: the namespaces contain the types, the types contain their
/// members, and the members contain their parameters, their accessors, and the locals, the local functions and the
/// lambdas declared in their body. It is built once per file and shared by the navigator and all its clones, as an
/// XPath evaluation clones the navigator for every step.
/// </summary>
internal sealed class SymbolForest
{
    /// <summary>A forest with no symbol, which is used to validate a query without a compilation.</summary>
    public static readonly SymbolForest Empty = new(syntaxTree: null, semanticModel: null, [], new Dictionary<ISymbol, SymbolElement>(SymbolEqualityComparer.Default), XPathAttributeFilter.All, CancellationToken.None);

    private readonly SyntaxTree? _syntaxTree;
    private readonly SemanticModel? _semanticModel;
    private readonly Dictionary<ISymbol, SymbolElement> _elements;
    private readonly XPathAttributeFilter _filter;
    private readonly CancellationToken _cancellationToken;

    private SymbolForest(SyntaxTree? syntaxTree, SemanticModel? semanticModel, SymbolElement[] roots, Dictionary<ISymbol, SymbolElement> elements, XPathAttributeFilter filter, CancellationToken cancellationToken)
    {
        _syntaxTree = syntaxTree;
        _semanticModel = semanticModel;
        Roots = roots;
        _elements = elements;
        _filter = filter;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// The symbols that have no parent in the file, in document order. They are the namespaces and the types of the
    /// global namespace, and the locals of the top-level statements.
    /// </summary>
    public SymbolElement[] Roots { get; }

    public CancellationToken CancellationToken => _cancellationToken;

    public static SymbolForest Create(SyntaxNode root, SemanticModel semanticModel, XPathAttributeFilter filter, CancellationToken cancellationToken)
    {
        var syntaxTree = root.SyntaxTree;
        var symbols = new List<ISymbol>();
        var elements = new Dictionary<ISymbol, SymbolElement>(SymbolEqualityComparer.Default);

        // The source assembly only contains the symbols of the compilation, not the ones of the references. The
        // namespaces and the types that are not declared in the file are pruned, so only the file is walked.
        foreach (var member in semanticModel.Compilation.Assembly.GlobalNamespace.GetMembers())
        {
            AddDeclaredSymbol(member);
        }

        // The locals, the local functions and the lambdas are not members of a type, so they are found in the bodies
        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = node switch
            {
                VariableDeclaratorSyntax { Parent.Parent: not BaseFieldDeclarationSyntax } => semanticModel.GetDeclaredSymbol(node, cancellationToken),
                SingleVariableDesignationSyntax or ForEachStatementSyntax or CatchDeclarationSyntax or LocalFunctionStatementSyntax => semanticModel.GetDeclaredSymbol(node, cancellationToken),
                AnonymousFunctionExpressionSyntax => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol,
                _ => null,
            };

            if (symbol is ILocalSymbol or IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction })
            {
                AddDeclaredSymbol(symbol);
            }
        }

        // The parents are resolved once all the symbols are known, as a local can be found before its containing lambda
        var roots = new List<SymbolElement>();
        var children = new Dictionary<SymbolElement, List<SymbolElement>>();
        foreach (var symbol in symbols)
        {
            var element = elements[symbol];
            element.Parent = FindParent(symbol);
            if (element.Parent is null)
            {
                roots.Add(element);
            }
            else
            {
                if (!children.TryGetValue(element.Parent, out var list))
                {
                    list = [];
                    children.Add(element.Parent, list);
                }

                list.Add(element);
            }
        }

        foreach (var pair in children)
        {
            pair.Key.Children = SortByPosition(pair.Value);
        }

        return new SymbolForest(syntaxTree, semanticModel, SortByPosition(roots), elements, filter, cancellationToken);

        void AddDeclaredSymbol(ISymbol symbol)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (symbol.IsImplicitlyDeclared || !TryGetFirstSpan(symbol, syntaxTree, out var span) || elements.ContainsKey(symbol))
                return;

            elements.Add(symbol, new SymbolElement(symbol, span.Start));
            symbols.Add(symbol);

            switch (symbol)
            {
                case INamespaceSymbol @namespace:
                    foreach (var member in @namespace.GetMembers())
                    {
                        AddDeclaredSymbol(member);
                    }

                    break;

                case INamedTypeSymbol type:
                    AddDeclaredSymbols(type.TypeParameters);

                    // The members include the accessors, whose parent is their property or their event
                    AddDeclaredSymbols(type.GetMembers());
                    break;

                case IMethodSymbol method:
                    AddDeclaredSymbols(method.TypeParameters);
                    AddDeclaredSymbols(method.Parameters);
                    break;

                case IPropertySymbol property:
                    AddDeclaredSymbols(property.Parameters);
                    break;
            }
        }

        void AddDeclaredSymbols<T>(ImmutableArray<T> members)
            where T : ISymbol
        {
            foreach (var member in members)
            {
                AddDeclaredSymbol(member);
            }
        }

        // The accessors are declared in their property or their event, and the other symbols in the first containing
        // symbol that is declared in the file. The symbols that have none, such as the global namespace or the implicit
        // method of the top-level statements, are not elements.
        SymbolElement? FindParent(ISymbol symbol)
        {
            if (symbol is IMethodSymbol { AssociatedSymbol: { } associatedSymbol } && elements.TryGetValue(associatedSymbol, out var associatedElement))
                return associatedElement;

            for (var containingSymbol = symbol.ContainingSymbol; containingSymbol is not null; containingSymbol = containingSymbol.ContainingSymbol)
            {
                if (elements.TryGetValue(containingSymbol, out var element))
                    return element;
            }

            return null;
        }
    }

    // The elements are sorted in document order, so the order of the elements does not depend on the order of the members
    private static SymbolElement[] SortByPosition(List<SymbolElement> elements)
    {
        if (elements.Count is 0)
            return [];

        var result = elements.OrderBy(element => element.Position).ToArray();
        for (var i = 0; i < result.Length; i++)
        {
            result[i].IndexInParent = i;
        }

        return result;
    }

    private static bool TryGetFirstSpan(ISymbol symbol, SyntaxTree syntaxTree, out TextSpan span)
    {
        foreach (var location in symbol.Locations)
        {
            if (location.SourceTree == syntaxTree)
            {
                span = location.SourceSpan;
                return true;
            }
        }

        span = default;
        return false;
    }

    /// <summary>
    /// The spans of the locations of a symbol in the file. A partial symbol can be declared several times in the same file.
    /// </summary>
    public ImmutableArray<TextSpan> GetSpans(SymbolElement element)
    {
        if (!element.Spans.IsDefault)
            return element.Spans;

        var spans = ImmutableArray.CreateBuilder<TextSpan>();
        foreach (var location in element.Symbol.Locations)
        {
            if (location.SourceTree == _syntaxTree)
            {
                spans.Add(location.SourceSpan);
            }
        }

        return element.Spans = spans.ToImmutable();
    }

    /// <summary>
    /// The syntax nodes that declare a symbol in the file. A partial symbol can be declared several times in the same file.
    /// </summary>
    public IEnumerable<SyntaxNode> GetSyntaxNodes(SymbolElement element)
    {
        foreach (var reference in element.Symbol.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree == _syntaxTree)
                yield return reference.GetSyntax(_cancellationToken);
        }
    }

    /// <summary>
    /// The element of the symbol a syntax node declares or refers to, if this symbol is declared in the file.
    /// </summary>
    public SymbolElement? FindElement(SyntaxNode node)
    {
        if (_semanticModel is null || node.SyntaxTree != _syntaxTree)
            return null;

        var symbol = _semanticModel.GetDeclaredSymbol(node, _cancellationToken) ?? _semanticModel.GetSymbolInfo(node, _cancellationToken).Symbol;
        if (symbol is null)
            return null;

        // A reference to a generic member is a constructed symbol, whereas the file declares its definition
        if (_elements.TryGetValue(symbol, out var element) || _elements.TryGetValue(symbol.OriginalDefinition, out element))
            return element;

        return null;
    }

    /// <summary>
    /// The attributes of a symbol. They are computed once per file, as a query that tests an attribute visits the
    /// same symbol several times.
    /// </summary>
    public XPathAttribute[] GetAttributes(SymbolElement element)
    {
        return element.Attributes ??= SymbolXPathNavigator.BuildAttributes(element.Symbol, GetSpans(element), _filter);
    }
}
