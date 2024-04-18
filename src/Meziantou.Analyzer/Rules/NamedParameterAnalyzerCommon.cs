using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class NamedParameterAnalyzerCommon
{
    public static int ArgumentIndex(ArgumentSyntax argument)
    {
        var argumentListExpression = argument.FirstAncestorOrSelf<ArgumentListSyntax>();
        if (argumentListExpression is null)
            return -1;

        for (var i = 0; i < argumentListExpression.Arguments.Count; i++)
        {
            if (argumentListExpression.Arguments[i] == argument)
                return i;
        }

        return -1;
    }
}
