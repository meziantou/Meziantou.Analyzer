using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Rules;

internal static class EqualityShouldBeCorrectlyImplementedAnalyzerCommon
{
    public static bool IsEqualsOfTMethod(IMethodSymbol symbol)
    {
        return symbol.Name == nameof(object.Equals) &&
        symbol.ReturnType.IsBoolean() &&
        symbol.Parameters.Length == 1 &&
        symbol.Parameters[0].Type.IsEqualTo(symbol.ContainingType) &&
        symbol.DeclaredAccessibility == Accessibility.Public &&
        !symbol.IsStatic;
    }
}
