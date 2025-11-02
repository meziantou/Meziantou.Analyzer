using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

internal record struct OverloadParameterType(ITypeSymbol? Symbol, bool AllowInherits = false);
