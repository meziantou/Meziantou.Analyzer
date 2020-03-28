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
                result.AddRange(allInterfaces);
                result.Add(namedType);
                return result;
            }

            return allInterfaces;
        }

        public static bool InheritsFrom(this ITypeSymbol classSymbol, ITypeSymbol baseClassType)
        {
            if (classSymbol == null || baseClassType == null)
                return false;

            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (baseClassType.IsEqualTo(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }

        public static bool Implements(this ITypeSymbol classSymbol, ITypeSymbol interfaceType)
        {
            if (interfaceType == null)
                return false;

            return classSymbol.AllInterfaces.Any(i => interfaceType.IsEqualTo(i));
        }

        public static bool HasAttribute(this ISymbol symbol, ITypeSymbol attributeType)
        {
            if (attributeType == null)
                return false;

            foreach (var attribute in symbol.GetAttributes())
            {
                if (attributeType.IsEqualTo(attribute.AttributeClass))
                    return true;
            }

            return false;
        }

        public static bool IsOrInheritFrom(this ITypeSymbol symbol, ITypeSymbol expectedType)
        {
            if (symbol == null || expectedType == null)
                return false;

            return symbol.IsEqualTo(expectedType) || symbol.InheritsFrom(expectedType);
        }

        public static bool IsEqualToAny(this ITypeSymbol symbol, params ITypeSymbol[] expectedTypes)
        {
            if (symbol == null || expectedTypes == null)
                return false;

            return expectedTypes.Any(t => t.IsEqualTo(symbol));
        }

        public static bool IsObject(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_Object;
        }

        public static bool IsString(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_String;
        }

        public static bool IsChar(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_Char;
        }

        public static bool IsInt32(this ITypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            return symbol.SpecialType == SpecialType.System_Int32;
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

        public static bool IsEnumeration(this ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.EnumUnderlyingType != null;
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

        public static bool IsUnitTestClass(this ITypeSymbol typeSymbol)
        {
            var attributes = typeSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                var type = attribute.AttributeClass;
                while (type != null)
                {
                    var ns = type.ContainingNamespace;
                    if (ns.IsNamespace(new[] { "Microsoft", "VisualStudio", "TestTools", "UnitTesting" }) ||
                        ns.IsNamespace(new[] { "NUnit", "Framework" }) ||
                        ns.IsNamespace(new[] { "Xunit" }))
                    {
                        return true;
                    }

                    type = type.BaseType;
                }
            }

            return false;
        }
    }
}
