using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Gives the types of the parameters of the methods that can be found when looking up a method by name, wherever the lookup
/// is done. Looking up the symbols in scope with the semantic model is expensive, and most methods have no overload with the
/// parameter an analyzer looks for, so <see cref="OverloadFinder"/> uses this index to skip the lookup. In the same way,
/// most types have no <c>GetAwaiter</c> method, so <c>AwaitableTypes</c> uses it to skip the lookup of that method.
/// <para>
/// The index does not replace the lookup, as it ignores the imported namespaces and the accessibility of the methods. It only
/// guarantees that a method that can be found by a lookup, wherever the lookup is done, is taken into account.
/// </para>
/// The index is shared by the analyzers, as it only depends on the compilation.
/// </summary>
internal sealed class OverloadCandidateIndex
{
    private const int MaxConstraintDepth = 8;

    private static readonly ConditionalWeakTable<Compilation, OverloadCandidateIndex> Instances = new();

    private readonly Compilation _compilation;
    private readonly Lazy<Dictionary<string, List<INamedTypeSymbol>>?> _extensionTypesByMemberName;
    private readonly ConcurrentDictionary<string, ImmutableArray<IMethodSymbol>> _extensionMethodsByName = new(StringComparer.Ordinal);

    // The same methods are invoked many times in a compilation, and by several analyzers
    private readonly BoundedCache<ParameterTypesKey, ParameterTypes> _parameterTypes = new(capacity: 8192);
    private readonly Func<ParameterTypesKey, ParameterTypes> _createParameterTypes;

    private OverloadCandidateIndex(Compilation compilation)
    {
        _compilation = compilation;
        _extensionTypesByMemberName = new(() => CreateExtensionTypesByMemberName(compilation));
        _createParameterTypes = CreateParameterTypes;
    }

    public static OverloadCandidateIndex Get(Compilation compilation)
    {
        return Instances.GetValue(compilation, static compilation => new OverloadCandidateIndex(compilation));
    }

    /// <summary>
    /// Gets the types of the parameters of the methods named <paramref name="methodName"/> that can be found by a lookup in
    /// <paramref name="containingType"/> or in <paramref name="reducedReceiverType"/>: the methods of the types, of their base
    /// types and of their interfaces, and the extension methods of the compilation and of its references.
    /// </summary>
    public ParameterTypes GetParameterTypes(INamedTypeSymbol containingType, ITypeSymbol? reducedReceiverType, string methodName)
    {
        return _parameterTypes.GetOrAdd(new ParameterTypesKey(containingType, reducedReceiverType, methodName), _createParameterTypes);
    }

    /// <summary>
    /// Indicates whether a lookup of the methods named <paramref name="methodName"/> in <paramref name="type"/> can find a
    /// method, wherever the lookup is done: a method of the type, of its base types or of its interfaces, or an extension
    /// method of the compilation or of its references that can be invoked on the type.
    /// </summary>
    public bool MightHaveMethod(ITypeSymbol type, string methodName)
    {
        var builder = new ParameterTypes.Builder();
        if (!AddTypeMethods(builder, type, methodName, depth: 0) || builder.HasMethod)
            return true;

        if (!TryGetExtensionMethods(methodName, out var extensionMethods))
            return true;

        foreach (var extensionMethod in extensionMethods)
        {
            // A lookup reduces the extension methods the same way, so it cannot find the ones that cannot be reduced.
            // The members of an extension block are not reduced, so they are all considered
            if (IsExtensionBlock(extensionMethod.ContainingType) || extensionMethod.ReduceExtensionMethod(type) is not null)
                return true;
        }

        return false;
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

    private ParameterTypes CreateParameterTypes(ParameterTypesKey key)
    {
        var builder = new ParameterTypes.Builder();
        if (!AddTypeMethods(builder, key.ContainingType, key.MethodName, depth: 0))
            return ParameterTypes.Unknown;

        if (key.ReducedReceiverType is not null && !AddTypeMethods(builder, key.ReducedReceiverType, key.MethodName, depth: 0))
            return ParameterTypes.Unknown;

        if (!TryGetExtensionMethods(key.MethodName, out var extensionMethods))
            return ParameterTypes.Unknown;

        foreach (var extensionMethod in extensionMethods)
        {
            builder.Add(extensionMethod);
        }

        return builder.Build();
    }

    /// <summary>
    /// Adds the methods that a member lookup in the type can find. Returns <see langword="false"/> when they are not known.
    /// </summary>
    private bool AddTypeMethods(ParameterTypes.Builder builder, ITypeSymbol type, string methodName, int depth)
    {
        switch (type)
        {
            case INamedTypeSymbol or IArrayTypeSymbol when type.TypeKind is not TypeKind.Error:
                for (var current = type; current is not null; current = current.BaseType)
                {
                    builder.AddRange(current.GetMembers(methodName));
                }

                foreach (var interfaceType in type.AllInterfaces)
                {
                    builder.AddRange(interfaceType.GetMembers(methodName));
                }

                // The members of System.Object are available on the interfaces
                if (type.TypeKind is TypeKind.Interface)
                {
                    builder.AddRange(_compilation.ObjectType.GetMembers(methodName));
                }

                return true;

            // The members of a type parameter are the ones of its constraints
            case ITypeParameterSymbol typeParameter when depth < MaxConstraintDepth:
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (!AddTypeMethods(builder, constraintType, methodName, depth + 1))
                        return false;
                }

                builder.AddRange(_compilation.ObjectType.GetMembers(methodName));
                builder.AddRange(_compilation.GetSpecialType(SpecialType.System_ValueType).GetMembers(methodName));
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the extension methods named <paramref name="name"/>, as they are declared. Returns <see langword="false"/> when
    /// they are not known.
    /// </summary>
    private bool TryGetExtensionMethods(string name, out ImmutableArray<IMethodSymbol> methods)
    {
        var typesByMemberName = _extensionTypesByMemberName.Value;
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
        if (!_extensionMethodsByName.TryGetValue(name, out methods))
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

            methods = _extensionMethodsByName.GetOrAdd(name, builder.ToImmutable());
        }

        return true;
    }

    /// <summary>
    /// Indexes, by member name, the types of the compilation and of its references that can declare extension methods.
    /// Only the member names of the types are read, so their members are not loaded.
    /// </summary>
    private static Dictionary<string, List<INamedTypeSymbol>>? CreateExtensionTypesByMemberName(Compilation compilation)
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

    private readonly struct ParameterTypesKey(INamedTypeSymbol containingType, ITypeSymbol? reducedReceiverType, string methodName) : IEquatable<ParameterTypesKey>
    {
        public INamedTypeSymbol ContainingType { get; } = containingType;
        public ITypeSymbol? ReducedReceiverType { get; } = reducedReceiverType;
        public string MethodName { get; } = methodName;

        public bool Equals(ParameterTypesKey other)
        {
            return string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
                && SymbolEqualityComparer.Default.Equals(ContainingType, other.ContainingType)
                && SymbolEqualityComparer.Default.Equals(ReducedReceiverType, other.ReducedReceiverType);
        }

        public override bool Equals(object? obj) => obj is ParameterTypesKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = StringComparer.Ordinal.GetHashCode(MethodName);
                hashCode = (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(ContainingType);
                return (hashCode * 397) ^ SymbolEqualityComparer.Default.GetHashCode(ReducedReceiverType);
            }
        }
    }

    /// <summary>
    /// The types of the parameters of a set of methods. A method can only be similar to another method with additional
    /// parameters when it has a parameter that can match each additional type, whatever the type arguments inferred for the
    /// method are. This is cheap to check, and it rejects most methods.
    /// </summary>
    internal sealed class ParameterTypes
    {
        private readonly ImmutableArray<ITypeSymbol> _namedTypes;
        private readonly Kinds _kinds;

        private ParameterTypes(ImmutableArray<ITypeSymbol> namedTypes, Kinds kinds)
        {
            _namedTypes = namedTypes;
            _kinds = kinds;
        }

        [Flags]
        private enum Kinds
        {
            None = 0,
            Method = 1,
            AnyType = 2,
            MethodTypeParameter = 4,
            Array = 8,
            Pointer = 16,
            FunctionPointer = 32,
        }

        /// <summary>The methods are not known, so any method can have a parameter of any type.</summary>
        public static ParameterTypes Unknown { get; } = new(ImmutableArray<ITypeSymbol>.Empty, Kinds.Method | Kinds.AnyType);

        private static ParameterTypes Empty { get; } = new(ImmutableArray<ITypeSymbol>.Empty, Kinds.None);

        public bool HasMethod => (_kinds & Kinds.Method) != Kinds.None;

        /// <summary>
        /// Same as <see cref="MightHaveParameterOfType(ITypeSymbol, bool)"/> for a single method, without collecting the types
        /// of its parameters. Both must classify the types the same way as <see cref="Builder.Add"/>.
        /// </summary>
        public static bool MightHaveParameterOfType(IMethodSymbol method, ITypeSymbol additionalType, bool isTypeArgument)
        {
            // The parameters of a reduced extension method are compared using the ones of the method it is reduced from
            foreach (var parameter in (method.ReducedFrom ?? method).Parameters)
            {
                var isCompatible = parameter.Type switch
                {
                    INamedTypeSymbol namedType => string.Equals(namedType.Name, additionalType.Name, StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, additionalType.OriginalDefinition),
                    ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } => isTypeArgument,
                    ITypeParameterSymbol typeParameter => IsExtensionBlock(typeParameter.ContainingType),
                    IArrayTypeSymbol => additionalType is IArrayTypeSymbol,
                    IPointerTypeSymbol => additionalType is IPointerTypeSymbol,
                    IFunctionPointerTypeSymbol => additionalType is IFunctionPointerTypeSymbol,
                    _ => true,
                };

                if (isCompatible)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether a parameter can match the additional type. <paramref name="isTypeArgument"/> indicates whether
        /// the additional type is a type argument of the method the overload is searched for, as a type parameter of a
        /// method is only compared to it.
        /// </summary>
        public bool MightHaveParameterOfType(ITypeSymbol additionalType, bool isTypeArgument)
        {
            if ((_kinds & Kinds.AnyType) != Kinds.None)
                return true;

            if (isTypeArgument && (_kinds & Kinds.MethodTypeParameter) != Kinds.None)
                return true;

            switch (additionalType)
            {
                // The type arguments can differ, as they can be the type parameters of the method
                case INamedTypeSymbol:
                    var additionalTypeDefinition = additionalType.OriginalDefinition;
                    foreach (var namedType in _namedTypes)
                    {
                        if (ReferenceEquals(namedType, additionalTypeDefinition) || (string.Equals(namedType.Name, additionalTypeDefinition.Name, StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(namedType, additionalTypeDefinition)))
                            return true;
                    }

                    return false;

                case IArrayTypeSymbol:
                    return (_kinds & Kinds.Array) != Kinds.None;

                case IPointerTypeSymbol:
                    return (_kinds & Kinds.Pointer) != Kinds.None;

                case IFunctionPointerTypeSymbol:
                    return (_kinds & Kinds.FunctionPointer) != Kinds.None;

                // A parameter whose type is the additional type, when it is a type parameter or dynamic, is not tracked
                default:
                    return true;
            }
        }

        internal sealed class Builder
        {
            private List<ITypeSymbol>? _namedTypes;
            private Kinds _kinds;

            public bool HasMethod => (_kinds & Kinds.Method) != Kinds.None;

            public void AddRange(ImmutableArray<ISymbol> members)
            {
                foreach (var member in members)
                {
                    if (member is IMethodSymbol method)
                    {
                        Add(method);
                    }
                }
            }

            public void Add(IMethodSymbol method)
            {
                _kinds |= Kinds.Method;

                // The parameters of a reduced extension method are compared using the ones of the method it is reduced from
                foreach (var parameter in (method.ReducedFrom ?? method).Parameters)
                {
                    switch (parameter.Type)
                    {
                        case INamedTypeSymbol namedType:
                            AddNamedType(namedType.OriginalDefinition);
                            break;

                        case ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }:
                            _kinds |= Kinds.MethodTypeParameter;
                            break;

                        // A type parameter of an extension block is inferred from the receiver, so it can be any type.
                        // A type parameter of a type is not substituted, so it cannot be the additional type.
                        case ITypeParameterSymbol typeParameter:
                            if (IsExtensionBlock(typeParameter.ContainingType))
                            {
                                _kinds |= Kinds.AnyType;
                            }

                            break;

                        case IArrayTypeSymbol:
                            _kinds |= Kinds.Array;
                            break;

                        case IPointerTypeSymbol:
                            _kinds |= Kinds.Pointer;
                            break;

                        case IFunctionPointerTypeSymbol:
                            _kinds |= Kinds.FunctionPointer;
                            break;

                        default:
                            _kinds |= Kinds.AnyType;
                            break;
                    }
                }
            }

            private void AddNamedType(ITypeSymbol type)
            {
                _namedTypes ??= [];
                foreach (var namedType in _namedTypes)
                {
                    if (ReferenceEquals(namedType, type) || SymbolEqualityComparer.Default.Equals(namedType, type))
                        return;
                }

                _namedTypes.Add(type);
            }

            public ParameterTypes Build()
            {
                if (_kinds == Kinds.None)
                    return Empty;

                return new ParameterTypes(_namedTypes is null ? ImmutableArray<ITypeSymbol>.Empty : ImmutableArray.CreateRange(_namedTypes), _kinds);
            }
        }
    }
}
