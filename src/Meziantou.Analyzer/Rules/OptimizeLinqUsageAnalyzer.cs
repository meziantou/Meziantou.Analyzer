using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static System.FormattableString;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeLinqUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_listMethodsRule = new(
        RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
        title: "Use direct methods instead of LINQ methods",
        messageFormat: "Use '{0}' instead of '{1}()'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods));

    private static readonly DiagnosticDescriptor s_indexerInsteadOfElementAtRule = new(
        RuleIdentifiers.UseIndexerInsteadOfElementAt,
        title: "Use indexer instead of LINQ methods",
        messageFormat: "Use '{0}' instead of '{1}()'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIndexerInsteadOfElementAt));

    private static readonly DiagnosticDescriptor s_combineLinqMethodsRule = new(
        RuleIdentifiers.OptimizeEnumerable_CombineMethods,
        title: "Combine LINQ methods",
        messageFormat: "Combine '{0}' with '{1}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_CombineMethods));

    private static readonly DiagnosticDescriptor s_duplicateOrderByMethodsRule = new(
        RuleIdentifiers.DuplicateEnumerable_OrderBy,
        title: "Remove useless OrderBy call",
        messageFormat: "Remove the first '{0}' method or use '{1}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DuplicateEnumerable_OrderBy));

    private static readonly DiagnosticDescriptor s_optimizeCountRule = new(
        RuleIdentifiers.OptimizeEnumerable_Count,
        title: "Optimize Enumerable.Count() usage",
        messageFormat: "{0}",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_Count));

    private static readonly DiagnosticDescriptor s_optimizeWhereAndOrderByRule = new(
        RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy,
        title: "Use Where before OrderBy",
        messageFormat: "Call 'Where' before '{0}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy));

    private static readonly DiagnosticDescriptor s_useCastInsteadOfSelect = new(
        RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect,
        title: "Use 'Cast' instead of 'Select' to cast",
        messageFormat: "Use 'Cast<{0}>' instead of 'Select' to cast",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        s_listMethodsRule,
        s_indexerInsteadOfElementAtRule,
        s_combineLinqMethodsRule,
        s_duplicateOrderByMethodsRule,
        s_optimizeCountRule,
        s_optimizeWhereAndOrderByRule,
        s_useCastInsteadOfSelect);

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

        var symbols = new List<INamedTypeSymbol>();
        symbols.AddIfNotNull(context.Compilation.GetBestTypeByMetadataName("System.Linq.Enumerable"));
        symbols.AddIfNotNull(context.Compilation.GetBestTypeByMetadataName("System.Linq.Queryable"));
        if (!symbols.Any())
            return;

        var method = operation.TargetMethod;
        if (!symbols.Contains(method.ContainingType))
            return;

        UseFindInsteadOfFirstOrDefault(context, operation);
        UseCountPropertyInsteadOfMethod(context, operation);
        UseIndexerInsteadOfElementAt(context, operation);
        CombineWhereWithNextMethod(context, operation, symbols);
        RemoveTwoConsecutiveOrderBy(context, operation, symbols);
        WhereShouldBeBeforeOrderBy(context, operation, symbols);
        OptimizeCountUsage(context, operation, symbols);
        UseCastInsteadOfSelect(context, operation);
    }

    private static ImmutableDictionary<string, string?> CreateProperties(OptimizeLinqUsageData data)
    {
        return ImmutableDictionary.Create<string, string?>().Add("Data", data.ToString());
    }

    private static void WhereShouldBeBeforeOrderBy(OperationAnalysisContext context, IInvocationOperation operation, List<INamedTypeSymbol> enumerableSymbols)
    {
        if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
            string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal))
        {
            var parent = GetParentLinqOperation(operation);
            if (parent != null && enumerableSymbols.Contains(parent.TargetMethod.ContainingType))
            {
                if (string.Equals(parent.TargetMethod.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.CombineWhereWithNextMethod)
                       .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                       .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                       .Add("MethodName", parent.TargetMethod.Name);

                    context.ReportDiagnostic(s_optimizeWhereAndOrderByRule, properties, parent, operation.TargetMethod.Name);
                }
            }
        }
    }

    private static void UseCountPropertyInsteadOfMethod(OperationAnalysisContext context, IInvocationOperation operation)
    {
        if (operation.Arguments.Length != 1)
            return;

        if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Count), StringComparison.Ordinal))
        {
            var collectionOfTSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.ICollection`1");
            var readOnlyCollectionOfTSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");
            if (collectionOfTSymbol == null && readOnlyCollectionOfTSymbol == null)
                return;

            var actualType = operation.Arguments[0].Value.GetActualType();
            if (actualType == null)
                return;

            if (actualType.TypeKind == TypeKind.Array)
            {
                var properties = CreateProperties(OptimizeLinqUsageData.UseLengthProperty);
                context.ReportDiagnostic(s_listMethodsRule, properties, operation, DiagnosticReportOptions.ReportOnMethodName, "Length", operation.TargetMethod.Name);
                return;
            }

            if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.IsEqualTo(collectionOfTSymbol) || i.OriginalDefinition.IsEqualTo(readOnlyCollectionOfTSymbol)))
            {
                // Ensure the Count property is not an explicit implementation
                var count = actualType.GetMembers("Count").OfType<IPropertySymbol>().FirstOrDefault(m => m.ExplicitInterfaceImplementations.Length == 0);
                if (count != null)
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.UseCountProperty);
                    context.ReportDiagnostic(s_listMethodsRule, properties, operation, DiagnosticReportOptions.ReportOnMethodName, "Count", operation.TargetMethod.Name);
                    return;
                }
            }
        }
        else if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.LongCount), StringComparison.Ordinal))
        {
            var actualType = operation.Arguments[0].Value.GetActualType();
            if (actualType != null && actualType.TypeKind == TypeKind.Array)
            {
                var properties = CreateProperties(OptimizeLinqUsageData.UseLongLengthProperty);
                context.ReportDiagnostic(s_listMethodsRule, properties, operation, DiagnosticReportOptions.ReportOnMethodName, "LongLength", operation.TargetMethod.Name);
            }
        }
    }

    private static void UseFindInsteadOfFirstOrDefault(OperationAnalysisContext context, IInvocationOperation operation)
    {
        if (!string.Equals(operation.TargetMethod.Name, nameof(Enumerable.FirstOrDefault), StringComparison.Ordinal))
            return;

        if (operation.Arguments.Length != 2)
            return;

        var listSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1");
        var firstArgumentType = operation.Arguments[0].Value.GetActualType();
        if (firstArgumentType == null)
            return;

        if (firstArgumentType.OriginalDefinition.IsEqualTo(listSymbol))
        {
            ImmutableDictionary<string, string?> properties;
            var predicateArgument = operation.Arguments[1].Value;
            if (predicateArgument is IDelegateCreationOperation)
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseFindMethod);
            }
            else
            {
                if (!context.Options.GetConfigurationValue(operation, s_listMethodsRule.Id + ".report_when_conversion_needed", defaultValue: false))
                    return;

                properties = CreateProperties(OptimizeLinqUsageData.UseFindMethodWithConversion);
            }

            context.ReportDiagnostic(s_listMethodsRule, properties, operation, DiagnosticReportOptions.ReportOnMethodName, "Find()", operation.TargetMethod.Name);
        }
    }

    private static void UseIndexerInsteadOfElementAt(OperationAnalysisContext context, IInvocationOperation operation)
    {
        ImmutableDictionary<string, string?>? properties = null;

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

        var listSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IList`1");
        var readOnlyListSymbol = context.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
        if (listSymbol == null && readOnlyListSymbol == null)
            return;

        var actualType = operation.Arguments[0].Value.GetActualType();
        if (actualType == null)
            return;

        if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.IsEqualTo(listSymbol) || i.OriginalDefinition.IsEqualTo(readOnlyListSymbol)))
        {
            context.ReportDiagnostic(s_indexerInsteadOfElementAtRule, properties, operation, DiagnosticReportOptions.ReportOnMethodName, "[]", operation.TargetMethod.Name);
        }
    }

    private static void CombineWhereWithNextMethod(OperationAnalysisContext context, IInvocationOperation operation, IReadOnlyCollection<INamedTypeSymbol> enumerableSymbols)
    {
        if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
        {
            // Cannot replace Where when using Func<TSource,int,bool>
            if (operation.TargetMethod.Parameters.Length != 2)
                return;

            if (operation.TargetMethod.Parameters[1].Type is not INamedTypeSymbol type)
                return;

            if (type.TypeArguments.Length == 3)
                return;

            // Check parent methods
            var parent = GetParentLinqOperation(operation);
            if (parent != null && enumerableSymbols.Contains(parent.TargetMethod.ContainingType, SymbolEqualityComparer.Default))
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

    private static void RemoveTwoConsecutiveOrderBy(OperationAnalysisContext context, IInvocationOperation operation, IReadOnlyCollection<INamedTypeSymbol> enumerableSymbols)
    {
        if (string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
            string.Equals(operation.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal) ||
            string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ThenBy), StringComparison.Ordinal) ||
            string.Equals(operation.TargetMethod.Name, nameof(Enumerable.ThenByDescending), StringComparison.Ordinal))
        {
            var parent = GetParentLinqOperation(operation);
            if (parent != null && enumerableSymbols.Contains(parent.TargetMethod.ContainingType, SymbolEqualityComparer.Default))
            {
                if (string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal) ||
                    string.Equals(parent.TargetMethod.Name, nameof(Enumerable.OrderByDescending), StringComparison.Ordinal))
                {
                    var expectedMethodName = parent.TargetMethod.Name.ReplaceOrdinal("OrderBy", "ThenBy");
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

    private static void OptimizeCountUsage(OperationAnalysisContext context, IInvocationOperation operation, IReadOnlyCollection<INamedTypeSymbol> enumerableSymbols)
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

        string? message = null;
        var properties = ImmutableDictionary<string, string?>.Empty;
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
                            .Add("SkipMinusOne", value: "");
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
                            .Add("SkipMinusOne", value: "");
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
                        .Add("SkipMinusOne", value: "");
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
                        .Add("SkipMinusOne", value: "");
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
            var isCountLeftOperand = binaryOperation.LeftOperand == countOperand;
            return binaryOperation.OperatorKind switch
            {
                BinaryOperatorKind.LessThan => isCountLeftOperand ? BinaryOperatorKind.LessThan : BinaryOperatorKind.GreaterThan,
                BinaryOperatorKind.LessThanOrEqual => isCountLeftOperand ? BinaryOperatorKind.LessThanOrEqual : BinaryOperatorKind.GreaterThanOrEqual,
                BinaryOperatorKind.GreaterThanOrEqual => isCountLeftOperand ? BinaryOperatorKind.GreaterThanOrEqual : BinaryOperatorKind.LessThanOrEqual,
                BinaryOperatorKind.GreaterThan => isCountLeftOperand ? BinaryOperatorKind.GreaterThan : BinaryOperatorKind.LessThan,
                _ => binaryOperation.OperatorKind,
            };
        }

        bool HasTake()
        {
            var op = GetChildLinqOperation(operation);
            if (op == null)
                return false;

            return string.Equals(op.TargetMethod.Name, nameof(Enumerable.Take), StringComparison.Ordinal) && enumerableSymbols.Contains(op.TargetMethod.ContainingType, SymbolEqualityComparer.Default);
        }
    }

    private static void UseCastInsteadOfSelect(OperationAnalysisContext context, IInvocationOperation operation)
    {
        var semanticModel = operation.SemanticModel!;

        if (!string.Equals(operation.TargetMethod.Name, nameof(Enumerable.Select), StringComparison.Ordinal))
            return;

        // A valid 'Select' operation always has 2 arguments, regardless of whether the underlying code syntax is
        // that of a call to an extension method:   source.Select(selector);
        // or to a static method:                   Enumerable.Select(source, selector);
        //  Operation's first argument  -> 'source'
        //  Operation's second argument -> 'selector'
        if (operation.Arguments.Length != 2)
            return;

        var selectorArg = operation.Arguments[1];

        var returnOp = selectorArg.Descendants().OfType<IReturnOperation>().FirstOrDefault();
        if (returnOp is null)
            return;

        // If what's returned is not a cast value or the cast is done by 'as' operator
        if (returnOp.ReturnedValue is not IConversionOperation castOp || castOp.IsTryCast || castOp.Type == null)
            return;

        // If the cast is not applied directly to the source element (one of the selector's arguments)
        if (castOp.Operand.Kind != OperationKind.ParameterReference)
            return;

        // Ensure the code is valid after replacement. The semantic may be different if you use Cast<T>() instead of Select(x => (T)x).
        // Current conversion: (Type)value
        // Cast<T>() conversion: (Type)(object)value
        if (!CanReplaceByCast(castOp))
            return;

        // Determine if we're casting to a nullable type.
        // TODO: Revisit this once https://github.com/dotnet/roslyn/pull/42403 is merged.
        var selectMethodSymbol = semanticModel.GetSymbolInfo(operation.Syntax).Symbol as IMethodSymbol;
        var nullableFlowState = selectMethodSymbol?.TypeArgumentNullableAnnotations[1] == NullableAnnotation.Annotated ?
            NullableFlowState.MaybeNull :
            NullableFlowState.None;

        // Get the cast type's minimally qualified name, in the current context
        var castType = castOp.Type.ToMinimalDisplayString(semanticModel, nullableFlowState, operation.Syntax.SpanStart);
        var properties = CreateProperties(OptimizeLinqUsageData.UseCastInsteadOfSelect)
           .Add("CastType", castType);

        context.ReportDiagnostic(s_useCastInsteadOfSelect, properties, operation, DiagnosticReportOptions.ReportOnMethodName, castType);

        static bool CanReplaceByCast(IConversionOperation op)
        {
            if (op.Conversion.IsUserDefined || op.Conversion.IsNumeric)
                return false;

            // Handle enums: source.Select<MyEnum, byte>(item => (byte)item);
            // Using Cast<T> is only possible when the enum underlying type is the same as the conversion type
            var operandActualType = op.Operand.GetActualType();
            var enumerationType = operandActualType.GetEnumerationType();
            if (enumerationType != null)
            {
                if (!enumerationType.IsEqualTo(op.Type))
                    return false;
            }

            return true;
        }
    }

    private static IInvocationOperation? GetParentLinqOperation(IOperation op)
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

    private static IInvocationOperation? GetChildLinqOperation(IInvocationOperation op)
    {
        if (op.Arguments.Length == 0)
            return null;

        var argument = op.Arguments[0].Value;
        if (argument is IInvocationOperation invocationOperation)
            return invocationOperation;

        return null;
    }

    private static IBinaryOperation? GetParentBinaryOperation(IOperation op, out IOperation? operand)
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
