using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Indexes the extension methods of a compilation and of its references by name. Looking up the extension methods in scope
/// with the semantic model is expensive, and most method names are not the name of any extension method, so
/// <see cref="OverloadFinder"/> uses this index to skip the lookup.
/// <para>
/// The index does not replace the lookup, as it ignores the imported namespaces and the accessibility of the methods. It only
/// guarantees that an extension method that can be found by a lookup, wherever the lookup is done, is one of the methods
/// returned for its name.
/// </para>
/// The index is shared by the analyzers, as it only depends on the compilation.
/// </summary>
internal sealed class ExtensionMethodIndex
{
    private static readonly ConditionalWeakTable<Compilation, ExtensionMethodIndex> Instances = new();

    private readonly Lazy<Dictionary<string, List<INamedTypeSymbol>>?> _typesByMemberName;
    private readonly ConcurrentDictionary<string, ImmutableArray<IMethodSymbol>> _methodsByName = new(StringComparer.Ordinal);

    private ExtensionMethodIndex(Compilation compilation)
    {
        _typesByMemberName = new(() => CreateTypesByMemberName(compilation));
    }

    public static ExtensionMethodIndex Get(Compilation compilation)
    {
        return Instances.GetValue(compilation, static compilation => new ExtensionMethodIndex(compilation));
    }

    /// <summary>
    /// Gets the extension methods named <paramref name="name"/>, as they are declared. Returns <see langword="false"/> when
    /// they are not known, so the extension methods must be looked up with the semantic model.
    /// </summary>
    public bool TryGetMethods(string name, out ImmutableArray<IMethodSymbol> methods)
    {
        var typesByMemberName = _typesByMemberName.Value;
        if (typesByMemberName is null)
        {
            methods = ImmutableArray<IMethodSymbol>.Empty;
            return false;
        }

        if (!typesByMemberName.TryGetValue(name, out var types))
        {
            methods = ImmutableArray<IMethodSymbol>.Empty;
            return true;
        }

        // Getting the members of a type declared in a reference loads all its members, so it is only done for the names that are requested
        if (!_methodsByName.TryGetValue(name, out methods))
        {
            var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();
            foreach (var type in types)
            {
                var isExtensionBlock = IsExtensionBlock(type);
                foreach (var member in type.GetMembers(name))
                {
                    if (member is IMethodSymbol method && (isExtensionBlock || method.IsExtensionMethod))
                    {
                        builder.Add(method);
                    }
                }
            }

            methods = _methodsByName.GetOrAdd(name, builder.ToImmutable());
        }

        return true;
    }

    /// <summary>Indicates whether the type is an <c>extension</c> block, whose members are all extension members.</summary>
    public static bool IsExtensionBlock([NotNullWhen(true)] INamedTypeSymbol? type)
    {
#if CSHARP14_OR_GREATER
        return type is { TypeKind: TypeKind.Extension };
#else
        return false;
#endif
    }

    /// <summary>
    /// Indexes, by member name, the types of the compilation and of its references that can declare extension methods.
    /// Only the member names of the types are read, so their members are not loaded.
    /// </summary>
    private static Dictionary<string, List<INamedTypeSymbol>>? CreateTypesByMemberName(Compilation compilation)
    {
        // A script can use the extension methods of the previous submissions, and the usings of the compilation options can import a type
        if (compilation.ScriptCompilationInfo is not null || compilation.Options is CSharpCompilationOptions { Usings.IsEmpty: false })
            return null;

        var result = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);

        // The global namespace of the compilation does not contain the types of the references that only have an extern alias,
        // while their namespaces can be imported
        var namespaces = new Stack<INamespaceSymbol>();
        AddAssembly(compilation.Assembly);
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            AddAssembly(assembly);
        }

        // A using static directive is the only way to import the extension methods of a nested type, which can be declared
        // by another language
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            if (syntaxTree.GetRoot() is not CompilationUnitSyntax root)
                continue;

            SemanticModel? semanticModel = null;
            AddUsingStaticDirectives(root.Usings, root.Members);

            void AddUsingStaticDirectives(SyntaxList<UsingDirectiveSyntax> usings, SyntaxList<MemberDeclarationSyntax> members)
            {
                foreach (var usingDirective in usings)
                {
                    if (!usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                        continue;

                    semanticModel ??= compilation.GetSemanticModel(syntaxTree);
                    var symbolInfo = semanticModel.GetSymbolInfo(usingDirective.NamespaceOrType);
                    AddNestedType(symbolInfo.Symbol);
                    foreach (var candidateSymbol in symbolInfo.CandidateSymbols)
                    {
                        AddNestedType(candidateSymbol);
                    }
                }

                foreach (var member in members)
                {
                    if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
                    {
                        AddUsingStaticDirectives(namespaceDeclaration.Usings, namespaceDeclaration.Members);
                    }
                }
            }
        }

        return result;

        void AddAssembly(IAssemblySymbol assembly)
        {
            if (!assembly.MightContainExtensionMethods)
                return;

            namespaces.Push(assembly.GlobalNamespace);
            while (namespaces.Count > 0)
            {
                foreach (var member in namespaces.Pop().GetMembers())
                {
                    if (member is INamespaceSymbol childNamespace)
                    {
                        namespaces.Push(childNamespace);
                    }
                    else if (member is INamedTypeSymbol type)
                    {
                        AddType(type);
                    }
                }
            }
        }

        void AddNestedType(ISymbol? symbol)
        {
            // The types that are not nested are indexed with their assembly
            if (symbol is INamedTypeSymbol { ContainingType: not null } type)
            {
                AddType(type);
            }
        }

        void AddType(INamedTypeSymbol type)
        {
            if (!type.MightContainExtensionMethods)
                return;

            AddMemberNames(type);

#if CSHARP14_OR_GREATER
            // An extension block is a type nested in the static class
            if (type is { IsStatic: true, IsGenericType: false })
            {
                foreach (var nestedType in type.GetTypeMembers())
                {
                    if (IsExtensionBlock(nestedType))
                    {
                        AddMemberNames(nestedType);
                    }
                }
            }
#endif
        }

        void AddMemberNames(INamedTypeSymbol type)
        {
            foreach (var memberName in type.MemberNames)
            {
                // The constructors (.ctor and .cctor) are not extension methods
                if (memberName is ['.', ..])
                    continue;

                if (!result.TryGetValue(memberName, out var types))
                {
                    types = [];
                    result.Add(memberName, types);
                }

                // A type has several members with the same name when the name is overloaded
                if (types.Count == 0 || !ReferenceEquals(types[^1], type))
                {
                    types.Add(type);
                }
            }
        }
    }
}
