using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer
{
    internal static class OperationExtensions
    {
        public static IEnumerable<IOperation> Ancestors(this IOperation operation)
        {
            operation = operation.Parent;
            while (operation != null)
            {
                yield return operation;
                operation = operation.Parent;
            }
        }

        public static bool IsInQueryableExpressionArgument(this IOperation operation)
        {
            foreach (var invocationOperation in operation.Ancestors().OfType<IInvocationOperation>())
            {
                var type = invocationOperation.TargetMethod.ContainingType;
                if (type.IsEqualsTo(operation.SemanticModel.Compilation.GetTypeByMetadataName("System.Linq.Queryable")))
                    return true;
            }

            return false;
        }
    }
}
