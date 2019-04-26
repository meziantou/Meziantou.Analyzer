using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static System.FormattableString;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OptimizeLinqUsageAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_listMethodsRule = new DiagnosticDescriptor(
            RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
            title: "Use direct methods instead of extension methods",
            messageFormat: "Use '{0}' instead of '{1}()'",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods));

        private static readonly DiagnosticDescriptor s_combineLinqMethodsRule = new DiagnosticDescriptor(
            RuleIdentifiers.OptimizeLinqUsage,
            title: "Optimize LINQ usage",
            messageFormat: "Combine '{0}' with '{1}'",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeLinqUsage));

        private static readonly DiagnosticDescriptor s_duplicateOrderByMethodsRule = new DiagnosticDescriptor(
            RuleIdentifiers.DuplicateEnumerable_OrderBy,
            title: "Optimize LINQ usage",
            messageFormat: "Remove the first '{0}' method or use '{1}'",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DuplicateEnumerable_OrderBy));

        private static readonly DiagnosticDescriptor s_optimizeCountRule = new DiagnosticDescriptor(
            RuleIdentifiers.OptimizeEnumerable_Count,
            title: "Optimize Enumerable.Count usage",
            messageFormat: "{0}",
            RuleCategories.Performance,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_Count));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_listMethodsRule, s_combineLinqMethodsRule, s_duplicateOrderByMethodsRule, s_optimizeCountRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Arguments.Length == 0)
                return;

            var enumerableSymbol = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableSymbol == null)
                return;

            var method = operation.TargetMethod;
            if (!method.ContainingType.IsEqualTo(enumerableSymbol))
                return;

            UseFindInsteadOfFirstOrDefault(context, operation);
            UseCountPropertyInsteadOfMethod(context, operation);
            UseIndexerInsteadOfElementAt(context, operation);
            CombineWhereWithNextMethod(context, operation, enumerableSymbol);
            RemoveTwoConsecutiveOrderBy(context, operation, enumerableSymbol);
            OptimizeCountUsage(context, operation, enumerableSymbol);
        }

        private static ImmutableDictionary<string, string> CreateProperties(OptimizeLinqUsageData data)
        {
            return ImmutableDictionary.Create<string, string>().Add("Data", data.ToString());
        }

        private static void UseCountPropertyInsteadOfMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.Arguments.Length != 1)
                return;

            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Count), StringComparison.Ordinal))
            {
                var collectionOfTSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
                var readOnlyCollectionOfTSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");
                if (collectionOfTSymbol == null && readOnlyCollectionOfTSymbol == null)
                    return;

                var actualType = GetActualType(operation.Arguments[0]);
                if (actualType.TypeKind == TypeKind.Array)
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.UseLengthProperty);
                    context.ReportDiagnostic(s_listMethodsRule, properties, operation, "Length", operation.TargetMethod.Name);
                    return;
                }

                if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.Equals(collectionOfTSymbol) || i.OriginalDefinition.Equals(readOnlyCollectionOfTSymbol)))
                {
                    // Ensure the Count property is not an explicit implementation
                    var count = actualType.GetMembers("Count").OfType<IPropertySymbol>().FirstOrDefault(m => m.ExplicitInterfaceImplementations.Length == 0);
                    if (count != null)
                    {
                        var properties = CreateProperties(OptimizeLinqUsageData.UseCountProperty);
                        context.ReportDiagnostic(s_listMethodsRule, properties, operation, "Count", operation.TargetMethod.Name);
                        return;
                    }
                }
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.LongCount), StringComparison.Ordinal))
            {
                var actualType = GetActualType(operation.Arguments[0]);
                if (actualType.TypeKind == TypeKind.Array)
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.UseLongLengthProperty);
                    context.ReportDiagnostic(s_listMethodsRule, properties, operation, "LongLength", operation.TargetMethod.Name);
                }
            }
        }

        private static void UseFindInsteadOfFirstOrDefault(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (!string.Equals(operation.TargetMethod.Name, nameof(Enumerable.FirstOrDefault), StringComparison.Ordinal))
                return;

            if (operation.Arguments.Length != 2)
                return;

            var listSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            if (GetActualType(operation.Arguments[0]).OriginalDefinition.IsEqualTo(listSymbol))
            {
                var properties = CreateProperties(OptimizeLinqUsageData.UseFindMethod);
                context.ReportDiagnostic(s_listMethodsRule, properties, operation, "Find()", operation.TargetMethod.Name);
            }
        }

        private static void UseIndexerInsteadOfElementAt(OperationAnalysisContext context, IInvocationOperation operation)
        {
            ImmutableDictionary<string, string> properties = default;

            var argCount = -1;
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ElementAt), StringComparison.Ordinal))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexer);
                argCount = 2;
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.First), StringComparison.Ordinal))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexerFirst);
                argCount = 1;
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Last), StringComparison.Ordinal))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexerLast);
                argCount = 1;
            }

            if (argCount < 0)
                return;

            if (operation.Arguments.Length != argCount)
                return;

            var listSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");
            var readOnlyListSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
            if (listSymbol == null && readOnlyListSymbol == null)
                return;

            var actualType = GetActualType(operation.Arguments[0]);
            if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.Equals(listSymbol) || i.OriginalDefinition.Equals(readOnlyListSymbol)))
            {
                context.ReportDiagnostic(s_listMethodsRule, properties, operation, "[]", operation.TargetMethod.Name);
            }
        }

        private static void CombineWhereWithNextMethod(OperationAnalysisContext context, IInvocationOperation operation, ITypeSymbol enumerableSymbol)
        {
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent != null && parent.TargetMethod.ContainingType.IsEqualTo(enumerableSymbol))
                {
                    if (string.Equals(parent.TargetMethod.Name, nameof(Enumerable.First), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.FirstOrDefault), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Last), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.LastOrDefault), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Single), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.SingleOrDefault), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Any), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Count), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.LongCount), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
                    {
                        var properties = CreateProperties(OptimizeLinqUsageData.CombineWhereWithNextMethod)
                           .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("MethodName", parent.TargetMethod.Name);

                        context.ReportDiagnostic(s_combineLinqMethodsRule, properties, parent, operation.TargetMethod.Name, parent.TargetMethod.Name);
                    }
                }
            }
        }

        private static void RemoveTwoConsecutiveOrderBy(OperationAnalysisContext context, IInvocationOperation operation, ITypeSymbol enumerableSymbol)
        {
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
                string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal) ||
                string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ThenBy), StringComparison.Ordinal) ||
                string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ThenByDescending), StringComparison.Ordinal))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent != null && parent.TargetMethod.ContainingType.IsEqualTo(enumerableSymbol))
                {
                    if (string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal))
                    {
                        var expectedMethodName = parent.TargetMethod.Name.Replace("OrderBy", "ThenBy");
                        var properties = CreateProperties(OptimizeLinqUsageData.DuplicatedOrderBy)
                            .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                            .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                            .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                            .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                            .Add("ExpectedMethodName", expectedMethodName)
                            .Add("MethodName", parent.TargetMethod.Name);

                        context.ReportDiagnostic(s_duplicateOrderByMethodsRule, properties, parent, operation.TargetMethod.Name, expectedMethodName);
                    }
                }
            }
        }

        private static void OptimizeCountUsage(OperationAnalysisContext context, IInvocationOperation operation, ITypeSymbol enumerableSymbol)
        {
            if (!string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Count), StringComparison.Ordinal))
                return;

            var binaryOperation = GetParentBinaryOperation(operation, out var countOperand);
            if (binaryOperation == null)
                return;

            if (!IsSupportedOperator(binaryOperation.OperatorKind))
                return;

            if (!binaryOperation.LeftOperand.Type.IsInt32() || !binaryOperation.RightOperand.Type.IsInt32())
                return;

            var opKind = NormalizeOperator();
            var otherOperand = binaryOperation.LeftOperand == countOperand ? binaryOperation.RightOperand : binaryOperation.LeftOperand;
            if (otherOperand == null)
                return;

            string message = null;
            var properties = ImmutableDictionary<string, string>.Empty;
            if (otherOperand.ConstantValue.HasValue && otherOperand.ConstantValue.Value is int value)
            {
                switch (opKind)
                {
                    case BinaryOperatorKind.Equals:
                        if (value < 0)
                        {
                            // expr.Count() == -1
                            message = "Expression is always false";
                            properties = CreateProperties(OptimizeLinqUsageData.UseFalse);
                        }
                        else if (value == 0)
                        {
                            // expr.Count() == 0
                            message = "Replace 'Count() == 0' with 'Any() == false'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseNotAny);
                        }
                        else
                        {
                            // expr.Count() == 1
                            if (!HasTake())
                            {
                                message = Invariant($"Replace 'Count() == {value}' with 'Take({value + 1}).Count() == {value}'");
                                properties = CreateProperties(OptimizeLinqUsageData.UseTakeAndCount);
                            }
                        }

                        break;

                    case BinaryOperatorKind.NotEquals:
                        if (value < 0)
                        {
                            // expr.Count() != -1 is always true
                            message = "Expression is always true";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTrue);
                        }
                        else if (value == 0)
                        {
                            // expr.Count() != 0
                            message = "Replace 'Count() != 0' with 'Any()'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseAny);
                        }
                        else
                        {
                            // expr.Count() != 1
                            if (!HasTake())
                            {
                                message = Invariant($"Replace 'Count() != {value}' with 'Take({value + 1}).Count() != {value}'");
                                properties = CreateProperties(OptimizeLinqUsageData.UseTakeAndCount);
                            }
                        }

                        break;

                    case BinaryOperatorKind.LessThan:
                        if (value <= 0)
                        {
                            // expr.Count() < 0
                            message = "Expression is always false";
                            properties = CreateProperties(OptimizeLinqUsageData.UseFalse);
                        }
                        else if (value == 1)
                        {
                            // expr.Count() < 1 ==> expr.Count() == 0
                            message = "Replace 'Count() < 1' with 'Any() == false'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseNotAny);
                        }
                        else
                        {
                            // expr.Count() < 10
                            message = Invariant($"Replace 'Count() < {value}' with 'Skip({value - 1}).Any() == false'");
                            properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndNotAny)
                                .Add("SkipMinusOne", null);
                        }

                        break;

                    case BinaryOperatorKind.LessThanOrEqual:
                        if (value < 0)
                        {
                            // expr.Count() <= -1
                            message = "Expression is always false";
                            properties = CreateProperties(OptimizeLinqUsageData.UseFalse);
                        }
                        else if (value == 0)
                        {
                            // expr.Count() <= 0
                            message = "Replace 'Count() <= 0' with 'Any() == false'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseNotAny);
                        }
                        else
                        {
                            // expr.Count() < 10
                            message = Invariant($"Replace 'Count() <= {value}' with 'Skip({value}).Any() == false'");
                            properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndNotAny);
                        }

                        break;

                    case BinaryOperatorKind.GreaterThan:
                        if (value < 0)
                        {
                            // expr.Count() > -1
                            message = "Expression is always true";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTrue);
                        }
                        else if (value == 0)
                        {
                            // expr.Count() > 0
                            message = "Replace 'Count() > 0' with 'Any()'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseAny);
                        }
                        else
                        {
                            // expr.Count() > 1
                            message = Invariant($"Replace 'Count() > {value}' with 'Skip({value}).Any()'");
                            properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndAny);
                        }

                        break;

                    case BinaryOperatorKind.GreaterThanOrEqual:
                        if (value <= 0)
                        {
                            // expr.Count() >= 0
                            message = "Expression is always true";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTrue);
                        }
                        else if (value == 1)
                        {
                            // expr.Count() >= 1
                            message = "Replace 'Count() >= 1' with 'Any()'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseAny);
                        }
                        else
                        {
                            // expr.Count() >= 2
                            message = Invariant($"Replace 'Count() >= {value}' with 'Skip({value - 1}).Any()'");
                            properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndAny)
                                .Add("SkipMinusOne", null);
                        }

                        break;
                }
            }
            else
            {
                switch (opKind)
                {
                    case BinaryOperatorKind.Equals:
                        // expr.Count() == 1
                        if (!HasTake())
                        {
                            message = "Replace 'Count() == n' with 'Take(n + 1).Count() == n'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTakeAndCount);
                        }

                        break;

                    case BinaryOperatorKind.NotEquals:
                        // expr.Count() != 1
                        if (!HasTake())
                        {
                            message = "Replace 'Count() != n' with 'Take(n + 1).Count() != n'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTakeAndCount);
                        }

                        break;

                    case BinaryOperatorKind.LessThan:
                        // expr.Count() < 10
                        message = "Replace 'Count() < n' with 'Skip(n - 1).Any() == false'";
                        properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndNotAny)
                            .Add("SkipMinusOne", null);
                        break;

                    case BinaryOperatorKind.LessThanOrEqual:
                        // expr.Count() <= 10
                        message = "Replace 'Count() <= n' with 'Skip(n).Any() == false'";
                        properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndNotAny);
                        break;

                    case BinaryOperatorKind.GreaterThan:
                        // expr.Count() > 1
                        message = "Replace 'Count() > n' with 'Skip(n).Any()'";
                        properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndAny);
                        break;

                    case BinaryOperatorKind.GreaterThanOrEqual:
                        // expr.Count() >= 2
                        message = "Replace 'Count() >= n' with 'Skip(n - 1).Any()'";
                        properties = CreateProperties(OptimizeLinqUsageData.UseSkipAndAny)
                            .Add("SkipMinusOne", null);
                        break;
                }
            }

            if (message != null)
            {
                properties = properties
                       .Add("OperandOperationStart", otherOperand.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("OperandOperationLength", otherOperand.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                       .Add("CountOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("CountOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture));

                context.ReportDiagnostic(s_optimizeCountRule, properties, binaryOperation, message);
            }

            static bool IsSupportedOperator(BinaryOperatorKind operatorKind)
            {
                switch (operatorKind)
                {
                    case BinaryOperatorKind.Equals:
                    case BinaryOperatorKind.NotEquals:
                    case BinaryOperatorKind.LessThan:
                    case BinaryOperatorKind.LessThanOrEqual:
                    case BinaryOperatorKind.GreaterThanOrEqual:
                    case BinaryOperatorKind.GreaterThan:
                        return true;
                    default:
                        return false;
                }
            }

            BinaryOperatorKind NormalizeOperator()
            {
                bool isCountLeftOperand = binaryOperation.LeftOperand == countOperand;
                switch (binaryOperation.OperatorKind)
                {
                    case BinaryOperatorKind.LessThan:
                        return isCountLeftOperand ? BinaryOperatorKind.LessThan : BinaryOperatorKind.GreaterThan;
                    case BinaryOperatorKind.LessThanOrEqual:
                        return isCountLeftOperand ? BinaryOperatorKind.LessThanOrEqual : BinaryOperatorKind.GreaterThanOrEqual;

                    case BinaryOperatorKind.GreaterThanOrEqual:
                        return isCountLeftOperand ? BinaryOperatorKind.GreaterThanOrEqual : BinaryOperatorKind.LessThanOrEqual;

                    case BinaryOperatorKind.GreaterThan:
                        return isCountLeftOperand ? BinaryOperatorKind.GreaterThan : BinaryOperatorKind.LessThan;

                    default:
                        return binaryOperation.OperatorKind;
                }
            }

            bool HasTake()
            {
                var op = GetChildLinqOperation(operation);
                if (op == null)
                    return false;

                return string.Equals(op.TargetMethod.Name, nameof(Enumerable.Take), StringComparison.Ordinal) && op.TargetMethod.ContainingType.Equals(enumerableSymbol);
            }
        }

        private static ITypeSymbol GetActualType(IArgumentOperation argument)
        {
            return argument.Value.GetActualType();
        }

        private static IInvocationOperation GetParentLinqOperation(IOperation op)
        {
            var parent = op.Parent;
            if (parent is IConversionOperation)
            {
                parent = parent.Parent;
            }

            if (parent is IInvocationOperation invocationOperation)
                return invocationOperation;

            if (parent is IArgumentOperation)
            {
                return GetParentLinqOperation(parent);
            }

            return null;
        }

        private static IInvocationOperation GetChildLinqOperation(IInvocationOperation op)
        {
            if (op.Arguments.Length == 0)
                return null;

            var argument = op.Arguments[0].Value;
            if (argument is IInvocationOperation invocationOperation)
                return invocationOperation;

            return null;
        }

        private static IBinaryOperation GetParentBinaryOperation(IOperation op, out IOperation operand)
        {
            var parent = op.Parent;
            if (parent is IConversionOperation)
            {
                op = parent;
                parent = parent.Parent;
            }

            if (parent is IBinaryOperation binaryOperation)
            {
                operand = op;
                return binaryOperation;
            }

            operand = null;
            return null;
        }
    }
}
