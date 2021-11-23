using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer;

internal static class CompilationExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation compilation, string typeMetadataName)
    {
        return compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
            .WhereNotNull();
    }
}
