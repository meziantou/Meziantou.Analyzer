using System.Collections.Concurrent;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Finds the values of a type that are available at an operation, such as the <see cref="CancellationToken"/> or the TimeProvider to forward to
/// a method. A value is a local, a parameter, a field or a property in scope, or one of their instance fields or properties.
/// </summary>
/// <param name="isSearchedType">Indicates whether a value of the type can be used.</param>
/// <param name="isIgnoredType">Indicates whether the members of the type must not be searched.</param>
/// <param name="additionalPaths">The paths of the values that are always available, whatever the operation.</param>
internal sealed class AvailableValueFinder(Func<ITypeSymbol, bool> isSearchedType, Func<ITypeSymbol, bool>? isIgnoredType = null, ImmutableArray<string> additionalPaths = default)
{
    private readonly ConcurrentDictionary<(ITypeSymbol Symbol, int MaxDepth), List<ISymbol[]>?> _membersByType = new();

    /// <summary>Returns the paths of the available values, sorted from the shortest to the longest.</summary>
    public string[] FindPaths(IOperation operation, CancellationToken cancellationToken)
    {
        var paths = new List<string>();
        foreach (var symbol in operation.LookupAvailableSymbols(cancellationToken))
        {
            if (symbol is IMethodSymbol or ITypeSymbol)
                continue;

            var symbolType = symbol.GetSymbolType();
            if (symbolType is null)
                continue;

            var members = GetMembers(symbolType, maxDepth: 1);
            if (members is null)
                continue;

            foreach (var member in members)
            {
                if (!AreAllSymbolsAccessibleFromOperation(member, operation))
                    continue;

                paths.Add(ComputeFullPath(symbol.Name, member));
            }
        }

        if (!additionalPaths.IsDefaultOrEmpty)
        {
            paths.AddRange(additionalPaths);
        }

        if (paths.Count == 0)
            return [];

        return [.. paths.OrderBy(value => value.Count(c => c == '.')).ThenBy(value => value, StringComparer.Ordinal)];

        static bool AreAllSymbolsAccessibleFromOperation(IEnumerable<ISymbol> symbols, IOperation operation)
        {
            foreach (var item in symbols)
            {
                if (!IsSymbolAccessibleFromOperation(item, operation))
                    return false;
            }

            return true;
        }

        static string ComputeFullPath(string prefix, IEnumerable<ISymbol> symbols)
        {
            var suffix = string.Join('.', symbols.Select(symbol => symbol.Name));
            if (string.IsNullOrEmpty(suffix))
                return prefix;

            return prefix + "." + suffix;
        }

        static bool IsSymbolAccessibleFromOperation(ISymbol symbol, IOperation operation)
        {
            // The value of a property is read, so its getter must be accessible
            if (symbol is IPropertySymbol { GetMethod: { } getMethod })
            {
                symbol = getMethod;
            }

            return operation.SemanticModel!.IsAccessible(operation.Syntax.Span.Start, symbol);
        }
    }

    private List<ISymbol[]>? GetMembers(ITypeSymbol symbol, int maxDepth)
    {
        return _membersByType.GetOrAdd((symbol, maxDepth), item =>
        {
            var (symbol, maxDepth) = item;

            if (maxDepth < 0)
                return null;

            // Quickly skips the types that Roslyn marks as special (System.Object, the primitives, System.String, the collection
            // interfaces, ...) as none of them can contain the searched type. The upper bound is the highest SpecialType value defined by
            // the oldest supported Roslyn version; special types added by newer versions are simply not skipped.
            if ((int)symbol.SpecialType is >= 1 and <= 45)
                return null;

            if (isIgnoredType is not null && isIgnoredType(symbol))
                return null;

            if (isSearchedType(symbol))
                return [[]];

            var result = new List<ISymbol[]>();
            var members = symbol.GetAllMembers(includeInterfaceMembers: true);
            foreach (var member in members)
            {
                // The members are accessed through an instance, so static members cannot be used
                if (member.IsImplicitlyDeclared || member.IsStatic)
                    continue;

                ITypeSymbol memberTypeSymbol;
                switch (member)
                {
                    case IPropertySymbol { IsIndexer: false, GetMethod: not null } propertySymbol:
                        memberTypeSymbol = propertySymbol.Type;
                        break;

                    case IFieldSymbol fieldSymbol:
                        memberTypeSymbol = fieldSymbol.Type;
                        break;

                    default:
                        continue;
                }

                if (isSearchedType(memberTypeSymbol))
                {
                    result.Add([member]);
                }
                else
                {
                    var typeMembers = GetMembers(memberTypeSymbol, maxDepth - 1);
                    if (typeMembers is not null)
                    {
                        foreach (var objectMember in typeMembers)
                        {
                            result.Add([member, .. objectMember]);
                        }
                    }
                }
            }

            return result;
        });
    }
}
