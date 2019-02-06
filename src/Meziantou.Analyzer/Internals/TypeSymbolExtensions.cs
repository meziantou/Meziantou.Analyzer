using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    // http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Shared/Extensions/ITypeSymbolExtensions.cs,190b4ed0932458fd,references
    internal static class TypeSymbolExtensions
    {
        public static IList<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
        {
            var allInterfaces = type.AllInterfaces;
            if (type is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Interface && !allInterfaces.Contains(namedType))
            {
                var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
                result.Add(namedType);
                result.AddRange(allInterfaces);
                return result;
            }

            return allInterfaces;
        }

        public static bool IsOfType(this ITypeSymbol symbol, ITypeSymbol expectedType)
        {
            if (expectedType == null)
                throw new System.ArgumentNullException(nameof(expectedType));

            if (symbol == null)
                return false;

            return expectedType.Equals(symbol);
        }

        public static bool IsString(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_String;
        }

        public static bool IsBoolean(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_Boolean;
        }
    }
}
