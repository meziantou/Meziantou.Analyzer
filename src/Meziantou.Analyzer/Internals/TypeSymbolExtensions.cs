using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

// http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ITypeSymbolExtensions.cs,190b4ed0932458fd,references
internal static class TypeSymbolExtensions
{
    private static readonly string[] Microsoft_VisualStudio_TestTools_UnitTesting = ["Microsoft", "VisualStudio", "TestTools", "UnitTesting"];
    private static readonly string[] NUnit_Framework = ["NUnit", "Framework"];
    private static readonly string[] Xunit = ["Xunit"];

    public static bool IsUnitTestClass(this ITypeSymbol typeSymbol)
    {
        var attributes = typeSymbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            var type = attribute.AttributeClass;
            while (type is not null)
            {
                var ns = type.ContainingNamespace;
                if (ns.MatchesNamespace(Microsoft_VisualStudio_TestTools_UnitTesting) ||
                    ns.MatchesNamespace(NUnit_Framework) ||
                    ns.MatchesNamespace(Xunit))
                {
                    return true;
                }

                type = type.BaseType;
            }
        }

        return false;
    }

#if ROSLYN_5_9_OR_GREATER
    /// <summary>Determines whether the type is a union type (<c>union</c> declaration or a type following the union pattern).</summary>
    public static bool IsUnionType([NotNullWhen(true)] this ITypeSymbol? typeSymbol)
    {
#pragma warning disable RSEXPERIMENTAL006 // Type unions are still experimental
        return typeSymbol is { IsUnion: true };
#pragma warning restore RSEXPERIMENTAL006
    }

    /// <summary>Gets the case types of a union type.</summary>
    /// <remarks>
    /// The case types are defined by the union creation members: the public constructors with a single by-value or <see langword="in"/> parameter,
    /// or the <c>Create</c> methods of the nested <c>IUnionMembers</c> interface when the type is a union member provider.
    /// </remarks>
    public static IEnumerable<ITypeSymbol> GetUnionCaseTypes(this ITypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers(WellKnownMemberNames.InstanceConstructorName))
        {
            if (member is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, Parameters: [{ RefKind: RefKind.None or RefKind.In } parameter] })
                yield return parameter.Type;
        }

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var unionMembersInterface in namedTypeSymbol.GetTypeMembers("IUnionMembers"))
            {
                foreach (var member in unionMembersInterface.GetMembers("Create"))
                {
                    if (member is IMethodSymbol { IsStatic: true, Parameters: [{ RefKind: RefKind.None or RefKind.In } parameter] })
                        yield return parameter.Type;
                }
            }
        }
    }
#endif
}
