using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeLinqUsage));

        private static readonly DiagnosticDescriptor s_duplicateOrderByMethodsRule = new DiagnosticDescriptor(
            RuleIdentifiers.DuplicateOrderBy,
            title: "Optimize LINQ usage",
            messageFormat: "Remove the first '{0}' method or use '{1}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DuplicateOrderBy));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_listMethodsRule, s_combineLinqMethodsRule, s_duplicateOrderByMethodsRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Arguments.Length == 0)
                return;

            var enumerableSymbol = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableSymbol == null)
                return;

            var method = operation.TargetMethod;
            if (!method.ContainingType.IsEqualsTo(enumerableSymbol))
                return;

            UseFindInsteadOfFirstOrDefault(context, operation);
            UseCountPropertyInsteadOfMethod(context, operation);
            UseIndexerInsteadOfElementAt(context, operation);
            CombineWhereWithNextMethod(context, operation, enumerableSymbol);
            RemoveTwoConsecutiveOrderBy(context, operation, enumerableSymbol);

            // TODO Count() < 0 => false
            // TODO Count() <= 0 => !Any()
            // TODO Count() == 0 => !Any()
            // TODO Count() > 0 => Any()
            // TODO Count() < 10 => .Any()
            // TODO Count() <= 10 => .Any()
            // TODO Count() >= 10 => .Skip(9).Any()
            // TODO Count() > 10 => .Skip(10).Any()
        }

        private void UseCountPropertyInsteadOfMethod(OperationAnalysisContext context, IInvocationOperation operation)
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
                    context.ReportDiagnostic(Diagnostic.Create(s_listMethodsRule, operation.Syntax.GetLocation(), "Length", operation.TargetMethod.Name));
                    return;
                }

                if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.Equals(collectionOfTSymbol) || i.OriginalDefinition.Equals(readOnlyCollectionOfTSymbol)))
                {
                    // Ensure the Count property is not an explicit implementation
                    var count = actualType.GetMembers("Count").OfType<IPropertySymbol>().FirstOrDefault(m => m.ExplicitInterfaceImplementations.Length == 0);
                    if (count != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_listMethodsRule, operation.Syntax.GetLocation(), "Count", operation.TargetMethod.Name));
                        return;
                    }
                }
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.LongCount), StringComparison.Ordinal))
            {
                var actualType = GetActualType(operation.Arguments[0]);
                if (actualType.TypeKind == TypeKind.Array)
                {
                    context.ReportDiagnostic(Diagnostic.Create(s_listMethodsRule, operation.Syntax.GetLocation(), "LongLength", operation.TargetMethod.Name));
                }
            }
        }

        private void UseFindInsteadOfFirstOrDefault(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (!string.Equals(operation.TargetMethod.Name, nameof(Enumerable.FirstOrDefault), StringComparison.Ordinal))
                return;

            if (operation.Arguments.Length != 2)
                return;

            var listSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
            if (GetActualType(operation.Arguments[0]).OriginalDefinition.IsEqualsTo(listSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_listMethodsRule, operation.Syntax.GetLocation(), "Find()", operation.TargetMethod.Name));
            }
        }

        private void UseIndexerInsteadOfElementAt(OperationAnalysisContext context, IInvocationOperation operation)
        {
            var argCount = -1;
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ElementAt), StringComparison.Ordinal))
            {
                argCount = 2;
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.First), StringComparison.Ordinal))
            {
                argCount = 1;
            }
            else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Last), StringComparison.Ordinal))
            {
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
                context.ReportDiagnostic(Diagnostic.Create(s_listMethodsRule, operation.Syntax.GetLocation(), "[]", operation.TargetMethod.Name));
            }
        }

        private void CombineWhereWithNextMethod(OperationAnalysisContext context, IInvocationOperation operation, ITypeSymbol enumerableSymbol)
        {
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent != null && parent.TargetMethod.ContainingType.IsEqualsTo(enumerableSymbol))
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
                        context.ReportDiagnostic(Diagnostic.Create(s_combineLinqMethodsRule, parent.Syntax.GetLocation(), operation.TargetMethod.Name, parent.TargetMethod.Name));
                    }
                }
            }
        }

        private void RemoveTwoConsecutiveOrderBy(OperationAnalysisContext context, IInvocationOperation operation, ITypeSymbol enumerableSymbol)
        {
            if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
                string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent != null && parent.TargetMethod.ContainingType.IsEqualsTo(enumerableSymbol))
                {
                    if (string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
                        string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_duplicateOrderByMethodsRule, parent.Syntax.GetLocation(), operation.TargetMethod.Name, parent.TargetMethod.Name.Replace("OrderBy", "ThenBy")));
                    }
                }
            }
        }

        private static ITypeSymbol GetActualType(IArgumentOperation argument)
        {
            var value = argument.Value;
            if (value is IConversionOperation conversionOperation)
            {
                value = conversionOperation.Operand;
            }

            return value.Type;
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
    }
}
