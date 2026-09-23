namespace Meziantou.Analyzer.Rules;

internal static class EventsShouldHaveProperArgumentsAnalyzerCommon
{
    /// <summary>
    /// Indicates whether <c>this</c> can be used at the position, which is not the case in a static member, or in a static
    /// lambda or local function.
    /// </summary>
    internal static bool CanUseThis(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
        while (symbol is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } function)
        {
            if (function.IsStatic)
                return false;

            symbol = function.ContainingSymbol;
        }

        return symbol is IMethodSymbol { IsStatic: false };
    }
}
