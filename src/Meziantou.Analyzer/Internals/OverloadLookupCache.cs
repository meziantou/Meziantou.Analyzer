using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Caches the member lookups done by <see cref="OverloadFinder"/>. A cache is meant to be created when the analysis of a named
/// type starts (<c>RegisterSymbolStartAction</c>) and shared by the actions of that type, so it is released with the type.
/// It is thread-safe because the members of a type can be analyzed concurrently.
/// </summary>
internal sealed class OverloadLookupCache
{
    private readonly ConcurrentDictionary<LookupKey, ImmutableArray<ISymbol>> _cache = new(LookupKeyComparer.Instance);

    // A member lookup only depends on the file and enclosing type declaration (accessibility, imported namespaces), the container and the name
    public ImmutableArray<ISymbol> LookupSymbols(SemanticModel semanticModel, SyntaxNode node, int position, ITypeSymbol container, string name, bool includeReducedExtensionMethods)
    {
        var scopeStart = node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>()?.SpanStart ?? -1;
        var key = new LookupKey(node.SyntaxTree, scopeStart, container, name, includeReducedExtensionMethods);
        if (!_cache.TryGetValue(key, out var symbols))
        {
            symbols = semanticModel.LookupSymbols(position, container, name, includeReducedExtensionMethods);
            _ = _cache.TryAdd(key, symbols);
        }

        return symbols;
    }

    private readonly record struct LookupKey(SyntaxTree Tree, int ScopeStart, ITypeSymbol Container, string Name, bool IncludeReducedExtensionMethods);

    private sealed class LookupKeyComparer : IEqualityComparer<LookupKey>
    {
        public static LookupKeyComparer Instance { get; } = new();

        public bool Equals(LookupKey x, LookupKey y)
        {
            return ReferenceEquals(x.Tree, y.Tree)
                && x.ScopeStart == y.ScopeStart
                && SymbolEqualityComparer.Default.Equals(x.Container, y.Container)
                && string.Equals(x.Name, y.Name, StringComparison.Ordinal)
                && x.IncludeReducedExtensionMethods == y.IncludeReducedExtensionMethods;
        }

        public int GetHashCode(LookupKey obj)
        {
            unchecked
            {
                var hashCode = RuntimeHelpers.GetHashCode(obj.Tree);
                hashCode = (hashCode * 397) ^ obj.ScopeStart;
                hashCode = (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.Container);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(obj.Name);
                return (hashCode * 397) ^ (obj.IncludeReducedExtensionMethods ? 1 : 0);
            }
        }
    }
}
