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
                if (type.IsEqualTo(operation.SemanticModel.Compilation.GetTypeByMetadataName("System.Linq.Queryable")))
                    return true;
            }

            return false;
        }

        public static bool IsInExpressionArgument(this IOperation operation)
        {
            foreach (var op in operation.Ancestors().OfType<IArgumentOperation>())
            {
                var type = op.Parameter.Type;
                if (type.InheritsFrom(operation.SemanticModel.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression")))
                    return true;
            }

            return false;
        }

        public static bool IsInNameofOperation(this IOperation operation)
        {
            return operation.Ancestors().OfType<INameOfOperation>().Any();
        }

        public static ITypeSymbol? GetActualType(this IOperation operation)
        {
            if (operation is IConversionOperation conversionOperation)
            {
                return GetActualType(conversionOperation.Operand);
            }

            return operation.Type;
        }

        public static bool HasArgumentOfType(this IInvocationOperation operation, ITypeSymbol argumentTypeSymbol)
        {
            foreach (var arg in operation.Arguments)
            {
                if (argumentTypeSymbol.IsEqualTo(arg.Value.Type))
                    return true;
            }

            return false;
        }
    }
}
