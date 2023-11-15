using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer;

internal sealed class OperationUtilities(Compilation compilation)
{
    private readonly INamedTypeSymbol? _expressionSymbol = compilation.GetBestTypeByMetadataName("System.Linq.Expressions.Expression");

    public bool IsInExpressionContext(IOperation operation)
    {
        if (_expressionSymbol == null)
            return false;

        foreach (var op in operation.Ancestors())
        {
            if (op is IArgumentOperation argumentOperation)
            {
                if (argumentOperation.Parameter == null)
                    continue;

                var type = argumentOperation.Parameter.Type;
                if (type.InheritsFrom(_expressionSymbol))
                    return true;
            }
            else if (op is IConversionOperation conversionOperation)
            {
                var type = conversionOperation.Type;
                if (type is null)
                    continue;

                if (type.InheritsFrom(_expressionSymbol))
                    return true;
            }
        }

        return false;
    }

}
