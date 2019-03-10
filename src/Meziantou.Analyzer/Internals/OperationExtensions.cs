using System.Collections.Generic;
using Microsoft.CodeAnalysis;

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
    }
}
