using System;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class CompilationExtensions
    {
        public static INamedTypeSymbol GetTypeByMetadataName(this Compilation compilation, Type type)
        {
            return compilation.GetTypeByMetadataName(type.FullName);
        }

        public static INamedTypeSymbol GetTypeByMetadataName<T>(this Compilation compilation)
        {
            return GetTypeByMetadataName(compilation, typeof(T));
        }
    }
}
