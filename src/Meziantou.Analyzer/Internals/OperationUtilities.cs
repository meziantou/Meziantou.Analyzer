namespace Meziantou.Analyzer.Internals;

internal sealed class OperationUtilities(Compilation compilation)
{
    private readonly INamedTypeSymbol? _expressionSymbol = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression");

    public bool IsInExpressionContext(IOperation operation)
    {
        if (_expressionSymbol is null)
            return false;

        for (var op = operation.Parent; op is not null; op = op.Parent)
        {
            switch (op)
            {
                case IArgumentOperation { Parameter: { } parameter } when parameter.Type.InheritsFrom(_expressionSymbol):
                    return true;

                case IConversionOperation { Type: { } type } when type.InheritsFrom(_expressionSymbol):
                    return true;

                // An expression tree can only be entered by converting a lambda, so the search can stop at the first
                // enclosing body that is not the body of a lambda (method body, local function body, nested block, ...)
                case IBlockOperation when op.Parent is not IAnonymousFunctionOperation:
                    return false;
            }
        }

        return false;
    }
}
