using System.Globalization;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// The context of the XPath queries of the banned syntax files. It defines the <c>semantic</c>, <c>operation</c> and
/// <c>symbol</c> prefixes, the <c>syntax</c> function, which returns the syntax nodes of the operations or of the
/// symbols it is given, the <c>symbol</c> function, which returns the symbols of the syntax nodes it is given, and the
/// semantic functions, which test the type or the symbol of the node they are evaluated on, such as <c>implements</c>
/// or <c>has-attribute</c>.
/// </summary>
internal sealed class BannedSyntaxXsltContext : XsltContext
{
    /// <summary>The name of the function that returns the syntax nodes of a set of operations or of symbols.</summary>
    public const string SyntaxFunctionName = "syntax";

    /// <summary>The name of the function that returns the symbols of a set of syntax nodes.</summary>
    public const string SymbolFunctionName = "symbol";

    /// <summary>The name of the function that indicates whether the type of a node implements an interface.</summary>
    public const string ImplementsFunctionName = "implements";

    /// <summary>The name of the function that indicates whether the type of a node derives from a class.</summary>
    public const string InheritsFromFunctionName = "inherits-from";

    /// <summary>The name of the function that indicates whether the type of a node is, derives from, or implements a type.</summary>
    public const string IsAssignableToFunctionName = "is-assignable-to";

    /// <summary>The name of the function that indicates whether the symbol of a node has an attribute.</summary>
    public const string HasAttributeFunctionName = "has-attribute";

    /// <summary>The name of the function that returns the attributes applied to the symbol of a node.</summary>
    public const string AttributesFunctionName = "attributes";

    /// <summary>The name of the function that returns or tests the name of the assembly that contains the symbol of a node.</summary>
    public const string ContainingAssemblyFunctionName = "containing-assembly";

    /// <summary>The name of the function that indicates whether the symbol of a node is declared in the compilation.</summary>
    public const string IsFromCurrentAssemblyFunctionName = "is-from-current-assembly";

    /// <summary>
    /// A context whose functions return nothing, which is used to validate a query without a compilation.
    /// </summary>
    public static readonly BannedSyntaxXsltContext Empty = new(syntaxNavigatorFactory: null, symbolNavigatorFactory: null, semanticModel: null, CancellationToken.None);

    private readonly Func<SyntaxNodeXPathNavigator>? _syntaxNavigatorFactory;
    private readonly Func<SymbolXPathNavigator>? _symbolNavigatorFactory;
    private readonly SemanticModel? _semanticModel;
    private readonly CancellationToken _cancellationToken;

    // A query visits the same node several times, and a file usually has several queries. The context is used by a
    // single analysis of a single file, which is not concurrent. The Empty context never fills them, as it has no
    // semantic model.
    private readonly Dictionary<SyntaxNode, ISymbol?> _nodeSymbols = new(ReferenceComparer<SyntaxNode>.Instance);
    private readonly Dictionary<SyntaxNode, ITypeSymbol?> _nodeTypes = new(ReferenceComparer<SyntaxNode>.Instance);
    private readonly Dictionary<(string Name, string? Format), XPathTypeNameMatcher?> _typeNameMatchers = [];
    private readonly Dictionary<ISymbol, AttributeDataForest> _attributeForests = new(SymbolEqualityComparer.Default);

    public BannedSyntaxXsltContext(Func<SyntaxNodeXPathNavigator>? syntaxNavigatorFactory, Func<SymbolXPathNavigator>? symbolNavigatorFactory, SemanticModel? semanticModel, CancellationToken cancellationToken)
        : base(new NameTable())
    {
        // The context resolves the prefixes when the expression is evaluated, so it defines the same ones as the
        // resolver that compiles it
        AddNamespace(XPathNamespaces.SemanticPrefix, XPathNamespaces.SemanticNamespaceUri);
        AddNamespace(XPathNamespaces.OperationPrefix, XPathNamespaces.OperationNamespaceUri);
        AddNamespace(XPathNamespaces.SymbolPrefix, XPathNamespaces.SymbolNamespaceUri);
        _syntaxNavigatorFactory = syntaxNavigatorFactory;
        _symbolNavigatorFactory = symbolNavigatorFactory;
        _semanticModel = semanticModel;
        _cancellationToken = cancellationToken;
    }

    public override bool Whitespace => false;

    /// <summary>
    /// Indicates whether a name is the one of a function that needs the semantic model, such as <c>implements</c>.
    /// </summary>
    public static bool IsSemanticFunctionName(string name) => name switch
    {
        ImplementsFunctionName or InheritsFromFunctionName or IsAssignableToFunctionName or HasAttributeFunctionName or AttributesFunctionName or ContainingAssemblyFunctionName or IsFromCurrentAssemblyFunctionName => true,
        _ => false,
    };

    /// <summary>
    /// Indicates whether a name is the one of a function whose first argument is the name of a type, and whose
    /// optional second argument is the format of this name.
    /// </summary>
    public static bool IsTypeNameFunctionName(string name) => name is ImplementsFunctionName or InheritsFromFunctionName or IsAssignableToFunctionName or HasAttributeFunctionName;

    public override bool PreserveWhitespace(XPathNavigator node) => false;

    public override int CompareDocument(string baseUri, string nextbaseUri) => string.CompareOrdinal(baseUri, nextbaseUri);

    // The queries have no variable. The base type is not annotated, and null is how it says that a name is unknown.
    public override IXsltContextVariable ResolveVariable(string prefix, string name) => null!;

    // An unknown function is reported, as the engine throws when it cannot be resolved
    public override IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] argTypes)
    {
        if (prefix.Length is not 0)
            return null!;

        IXsltContextFunction? function = name switch
        {
            SyntaxFunctionName => new SyntaxFunction(_syntaxNavigatorFactory),
            SymbolFunctionName => new SymbolFunction(_symbolNavigatorFactory),
            ImplementsFunctionName => new TypeRelationFunction(this, TypeRelation.Implements),
            InheritsFromFunctionName => new TypeRelationFunction(this, TypeRelation.InheritsFrom),
            IsAssignableToFunctionName => new TypeRelationFunction(this, TypeRelation.IsAssignableTo),
            HasAttributeFunctionName => new HasAttributeFunction(this),
            AttributesFunctionName => new AttributesFunction(this),

            // A function has a single return type, so the name of the assembly and the test of the name are two functions
            ContainingAssemblyFunctionName => argTypes.Length is 0 ? new ContainingAssemblyNameFunction(this) : new ContainingAssemblyTestFunction(this),
            IsFromCurrentAssemblyFunctionName => new IsFromCurrentAssemblyFunction(this),
            _ => null,
        };

        // The engine does not check the number of arguments of the functions of a context, and a function called with
        // the wrong number of arguments would silently select nothing
        if (function is not null && (argTypes.Length < function.Minargs || argTypes.Length > function.Maxargs))
        {
            var expected = function.Minargs == function.Maxargs
                ? function.Minargs.ToString(CultureInfo.InvariantCulture)
                : function.Minargs.ToString(CultureInfo.InvariantCulture) + " to " + function.Maxargs.ToString(CultureInfo.InvariantCulture);
            throw new XPathException($"The '{name}' function takes {expected} argument(s), but {argTypes.Length.ToString(CultureInfo.InvariantCulture)} are given");
        }

        return function!;
    }

    // The symbol of a syntax node is the one of the 'semantic:Symbol' attribute, the symbol of an operation is the one
    // it refers to, and the symbol of an attribute is its class. When the navigator is on an attribute of an element,
    // the symbol is the one of the element.
    private ISymbol? GetContextSymbol(XPathNavigator navigator)
    {
        if (_semanticModel is null)
            return null;

        return navigator switch
        {
            SyntaxNodeXPathNavigator { Node: { } node } => GetNodeSymbol(node),
            OperationXPathNavigator { Operation: { } operation } => GetReferencedSymbol(operation),
            SymbolXPathNavigator { Element: { } element } => element.Symbol,
            AttributeDataXPathNavigator { Element: { IsAttribute: true } element } => element.Attribute.AttributeClass,
            _ => null,
        };
    }

    // The type of a node is the type of the expression, or the type the node declares or refers to. The type of an
    // argument of an attribute is the type of its value.
    private ITypeSymbol? GetContextType(XPathNavigator navigator)
    {
        if (_semanticModel is null)
            return null;

        return navigator switch
        {
            SyntaxNodeXPathNavigator { Node: { } node } => GetNodeType(node),
            OperationXPathNavigator { Operation: { } operation } => operation.Type,
            SymbolXPathNavigator { Element: { } element } => GetTypeOfSymbol(element.Symbol),
            AttributeDataXPathNavigator { Element: { } element } => element.IsAttribute ? element.Attribute.AttributeClass : element.Constant.Type,
            _ => null,
        };
    }

    private ISymbol? GetNodeSymbol(SyntaxNode node)
    {
        if (_nodeSymbols.TryGetValue(node, out var symbol))
            return symbol;

        // The syntax tree of the 'syntax' function is the one of the semantic model, but this is not the case of a
        // document built without a compilation
        if (node.SyntaxTree == _semanticModel!.SyntaxTree)
        {
            var symbolInfo = _semanticModel.GetSymbolInfo(node, _cancellationToken);
            symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault() ?? _semanticModel.GetDeclaredSymbol(node, _cancellationToken);
        }

        _nodeSymbols.Add(node, symbol);
        return symbol;
    }

    private ITypeSymbol? GetNodeType(SyntaxNode node)
    {
        if (_nodeTypes.TryGetValue(node, out var type))
            return type;

        if (node.SyntaxTree == _semanticModel!.SyntaxTree)
        {
            type = _semanticModel.GetTypeInfo(node, _cancellationToken).Type ?? GetTypeOfSymbol(GetNodeSymbol(node));
        }

        _nodeTypes.Add(node, type);
        return type;
    }

    private static ISymbol? GetReferencedSymbol(IOperation operation) => operation switch
    {
        IInvocationOperation invocation => invocation.TargetMethod,
        IObjectCreationOperation objectCreation => objectCreation.Constructor,
        IMemberReferenceOperation memberReference => memberReference.Member,
        ILocalReferenceOperation localReference => localReference.Local,
        IParameterReferenceOperation parameterReference => parameterReference.Parameter,
        _ => null,
    };

    // A type is its own type, and a method has no type, as its return type is not the type of the method
    private static ITypeSymbol? GetTypeOfSymbol(ISymbol? symbol) => symbol switch
    {
        ITypeSymbol type => type,
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        IEventSymbol @event => @event.Type,
        IParameterSymbol parameter => parameter.Type,
        ILocalSymbol local => local.Type,
        _ => null,
    };

    private XPathTypeNameMatcher? GetTypeNameMatcher(object[] args)
    {
        if (args.Length is 0)
            return null;

        var name = ToXPathString(args[0]);
        var format = args.Length > 1 ? ToXPathString(args[1]) : null;
        if (!_typeNameMatchers.TryGetValue((name, format), out var matcher))
        {
            matcher = XPathTypeNameMatcher.Create(name, format);
            _typeNameMatchers.Add((name, format), matcher);
        }

        return matcher;
    }

    private AttributeDataForest GetAttributeForest(ISymbol symbol)
    {
        if (!_attributeForests.TryGetValue(symbol, out var forest))
        {
            forest = AttributeDataForest.Create(symbol, _semanticModel?.SyntaxTree, _cancellationToken);
            _attributeForests.Add(symbol, forest);
        }

        return forest;
    }

    // The arguments are not converted to the types the function declares, so a node-set is converted to the value of
    // its first node, as the 'string' function does
    private static string ToXPathString(object value) => value switch
    {
        string text => text,
        bool boolean => XPathAttributeFormatter.ToXPathBoolean(boolean),
        double number => number.ToString(CultureInfo.InvariantCulture),
        XPathNodeIterator iterator => iterator.MoveNext() && iterator.Current is { } current ? current.Value : "",
        _ => value.ToString() ?? "",
    };

    // A type parameter has no base type and no interface of its own, so they are the ones of its constraints
    private static bool InheritsFrom(ITypeSymbol type, XPathTypeNameMatcher matcher)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                if (constraintType.TypeKind is TypeKind.Interface)
                    continue;

                if ((constraintType is not ITypeParameterSymbol && matcher.Matches(constraintType)) || InheritsFrom(constraintType, matcher))
                    return true;
            }

            return false;
        }

        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (matcher.Matches(baseType))
                return true;
        }

        return false;
    }

    private static bool Implements(ITypeSymbol type, XPathTypeNameMatcher matcher)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                if ((constraintType.TypeKind is TypeKind.Interface && matcher.Matches(constraintType)) || Implements(constraintType, matcher))
                    return true;
            }

            return false;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (matcher.Matches(@interface))
                return true;
        }

        return false;
    }

    private sealed class SyntaxFunction(Func<SyntaxNodeXPathNavigator>? syntaxNavigatorFactory) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.NodeSet;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.NodeSet];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (syntaxNavigatorFactory is null || args is not [XPathNodeIterator items])
                return NavigatorIterator.Empty;

            // Several operations can share the same syntax node, such as an expression and the conversion that wraps
            // it, and a partial symbol has several syntax nodes
            var nodes = new List<SyntaxNode>();
            var visited = new HashSet<SyntaxNode>();
            while (items.MoveNext())
            {
                switch (items.Current)
                {
                    case OperationXPathNavigator { Operation: { } operation }:
                        if (visited.Add(operation.Syntax))
                        {
                            nodes.Add(operation.Syntax);
                        }

                        break;

                    case SymbolXPathNavigator { Element: { } element } navigator:
                        foreach (var node in navigator.Forest.GetSyntaxNodes(element))
                        {
                            if (visited.Add(node))
                            {
                                nodes.Add(node);
                            }
                        }

                        break;

                    // An argument of an attribute has no syntax of its own, as the arguments of a 'params' array are
                    // several nodes, so it is the syntax of its attribute
                    case AttributeDataXPathNavigator { Element: { } element } navigator:
                        if (navigator.Forest.GetSyntaxNode(element) is { } attributeNode && visited.Add(attributeNode))
                        {
                            nodes.Add(attributeNode);
                        }

                        break;
                }
            }

            if (nodes.Count is 0)
                return NavigatorIterator.Empty;

            var syntaxNavigator = syntaxNavigatorFactory();
            var navigators = new List<XPathNavigator>(nodes.Count);
            foreach (var node in nodes)
            {
                navigators.Add(syntaxNavigator.CreateAt(node));
            }

            return new NavigatorIterator(navigators, index: -1);
        }
    }

    private sealed class SymbolFunction(Func<SymbolXPathNavigator>? symbolNavigatorFactory) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.NodeSet;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.NodeSet];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (symbolNavigatorFactory is null || args is not [XPathNodeIterator items])
                return NavigatorIterator.Empty;

            SymbolXPathNavigator? symbolNavigator = null;
            List<XPathNavigator>? navigators = null;

            // Several nodes can declare or refer to the same symbol, such as the parts of a partial type
            var visited = new HashSet<SymbolElement>();
            while (items.MoveNext())
            {
                if (items.Current is not SyntaxNodeXPathNavigator { Node: { } node })
                    continue;

                symbolNavigator ??= symbolNavigatorFactory();
                if (symbolNavigator.Forest.FindElement(node) is { } element && visited.Add(element))
                {
                    navigators ??= [];
                    navigators.Add(symbolNavigator.CreateAt(element));
                }
            }

            return navigators is null ? NavigatorIterator.Empty : new NavigatorIterator(navigators, index: -1);
        }
    }

    private enum TypeRelation
    {
        Implements,
        InheritsFrom,
        IsAssignableTo,
    }

    // implements(name[, format]), inherits-from(name[, format]) and is-assignable-to(name[, format])
    private sealed class TypeRelationFunction(BannedSyntaxXsltContext context, TypeRelation relation) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 2;

        public XPathResultType ReturnType => XPathResultType.Boolean;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.String, XPathResultType.String];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (context.GetContextType(docContext) is not { } type || context.GetTypeNameMatcher(args) is not { } matcher)
                return false;

            return relation switch
            {
                TypeRelation.Implements => Implements(type, matcher),
                TypeRelation.InheritsFrom => InheritsFrom(type, matcher),
                _ => matcher.Matches(type) || InheritsFrom(type, matcher) || Implements(type, matcher),
            };
        }
    }

    // has-attribute(name[, format]) is true when the class of an attribute is the type or derives from it
    private sealed class HasAttributeFunction(BannedSyntaxXsltContext context) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 2;

        public XPathResultType ReturnType => XPathResultType.Boolean;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.String, XPathResultType.String];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (context.GetContextSymbol(docContext) is not { } symbol || context.GetTypeNameMatcher(args) is not { } matcher)
                return false;

            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass is { } attributeClass && (matcher.Matches(attributeClass) || InheritsFrom(attributeClass, matcher)))
                    return true;
            }

            return false;
        }
    }

    // attributes() returns the attributes of the symbol of the context node, and attributes(nodes) the ones of the
    // symbols of the nodes, which is how a query selects them outside of a predicate
    private sealed class AttributesFunction(BannedSyntaxXsltContext context) : IXsltContextFunction
    {
        public int Minargs => 0;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.NodeSet;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.NodeSet];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            List<XPathNavigator>? navigators = null;
            if (args.Length is 0)
            {
                Add(docContext, visited: null);
            }
            else if (args is [XPathNodeIterator items])
            {
                // Several nodes can refer to the same symbol, whose attributes are only returned once
                var visited = new HashSet<AttributeDataForest>();
                while (items.MoveNext())
                {
                    if (items.Current is { } current)
                    {
                        Add(current, visited);
                    }
                }
            }

            return navigators is null ? NavigatorIterator.Empty : new NavigatorIterator(navigators, index: -1);

            void Add(XPathNavigator navigator, HashSet<AttributeDataForest>? visited)
            {
                if (context.GetContextSymbol(navigator) is not { } symbol)
                    return;

                var forest = context.GetAttributeForest(symbol);
                if (forest.Roots.Length is 0 || (visited is not null && !visited.Add(forest)))
                    return;

                var attributeNavigator = new AttributeDataXPathNavigator(forest);
                navigators ??= [];
                foreach (var root in forest.Roots)
                {
                    navigators.Add(attributeNavigator.CreateAt(root));
                }
            }
        }
    }

    // containing-assembly() returns the name of the assembly, or an empty string when there is none
    private sealed class ContainingAssemblyNameFunction(BannedSyntaxXsltContext context) : IXsltContextFunction
    {
        public int Minargs => 0;

        public int Maxargs => 0;

        public XPathResultType ReturnType => XPathResultType.String;

        public XPathResultType[] ArgTypes { get; } = [];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            return context.GetContextSymbol(docContext)?.ContainingAssembly?.Name ?? "";
        }
    }

    // containing-assembly(name) compares the names ignoring the case, as the names of the assemblies are
    private sealed class ContainingAssemblyTestFunction(BannedSyntaxXsltContext context) : IXsltContextFunction
    {
        public int Minargs => 1;

        public int Maxargs => 1;

        public XPathResultType ReturnType => XPathResultType.Boolean;

        public XPathResultType[] ArgTypes { get; } = [XPathResultType.String];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (args is not [var name] || context.GetContextSymbol(docContext)?.ContainingAssembly?.Name is not { } assemblyName)
                return false;

            return string.Equals(assemblyName, ToXPathString(name), StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class IsFromCurrentAssemblyFunction(BannedSyntaxXsltContext context) : IXsltContextFunction
    {
        public int Minargs => 0;

        public int Maxargs => 0;

        public XPathResultType ReturnType => XPathResultType.Boolean;

        public XPathResultType[] ArgTypes { get; } = [];

        public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext)
        {
            if (context._semanticModel is null || context.GetContextSymbol(docContext)?.ContainingAssembly is not { } assembly)
                return false;

            return SymbolEqualityComparer.Default.Equals(assembly, context._semanticModel.Compilation.Assembly);
        }
    }

    // The navigators of the list are never moved, as each iterator exposes its own copy of the current one
    private sealed class NavigatorIterator : XPathNodeIterator
    {
        public static readonly NavigatorIterator Empty = new([], index: -1);

        private readonly List<XPathNavigator> _navigators;
        private XPathNavigator? _current;
        private int _index;

        public NavigatorIterator(List<XPathNavigator> navigators, int index)
        {
            _navigators = navigators;
            _index = index;
            if (index >= 0 && index < navigators.Count)
            {
                _current = navigators[index].Clone();
            }
        }

        public override XPathNavigator? Current => _current;

        public override int CurrentPosition => _index + 1;

        public override int Count => _navigators.Count;

        public override XPathNodeIterator Clone() => new NavigatorIterator(_navigators, _index);

        public override bool MoveNext()
        {
            if (_index + 1 >= _navigators.Count)
                return false;

            _index++;
            _current = _navigators[_index].Clone();
            return true;
        }
    }
}
