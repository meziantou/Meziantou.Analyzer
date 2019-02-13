using System.Collections.Generic;
using System.Linq;
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

        public static bool InheritsFrom(this ITypeSymbol classSymbol, ITypeSymbol baseClassType)
        {
            if (baseClassType == null)
                return false;

            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (baseClassType.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }

        public static bool Implements(this ITypeSymbol classSymbol, ITypeSymbol interfaceType)
        {
            if (interfaceType == null)
                return false;

            return classSymbol.AllInterfaces.Any(i => interfaceType.Equals(i));
        }

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attributeType)
        {
            if (attributeType == null)
                return false;

            var attributes = symbol.GetAttributes();
            if (attributes == null)
                return false;

            foreach (var attribute in attributes)
            {
                if (attributeType.Equals(attribute.AttributeClass))
                    return true;
            }

            return false;
        }

        public static bool IsEqualsTo(this ITypeSymbol symbol, ITypeSymbol expectedType)
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

        public static bool IsDateTime(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_DateTime;
        }

        public static bool IsNumberType(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            switch (symbol.SpecialType)
            {
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    return true;

                default:
                    return false;
            }
        }
    }
}
