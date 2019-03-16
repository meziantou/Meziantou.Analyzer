using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    internal static class SymbolAnalysisContextExtensions
    {
        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }
        }
    }
}
