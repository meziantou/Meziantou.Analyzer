using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static System.FormattableString;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeLinqUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ListMethodsRule = new(
        RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods,
        title: "Use direct methods instead of LINQ methods",
        messageFormat: "Use '{0}' instead of '{1}()'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseListOfTMethodsInsteadOfEnumerableExtensionMethods));

    private static readonly DiagnosticDescriptor IndexerInsteadOfElementAtRule = new(
        RuleIdentifiers.UseIndexerInsteadOfElementAt,
        title: "Use indexer instead of LINQ methods",
        messageFormat: "Use '{0}' instead of '{1}()'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIndexerInsteadOfElementAt));

    private static readonly DiagnosticDescriptor CombineLinqMethodsRule = new(
        RuleIdentifiers.OptimizeEnumerable_CombineMethods,
        title: "Combine LINQ methods",
        messageFormat: "Combine '{0}' with '{1}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_CombineMethods));

    private static readonly DiagnosticDescriptor DuplicateOrderByMethodsRule = new(
        RuleIdentifiers.DuplicateEnumerable_OrderBy,
        title: "Remove useless OrderBy call",
        messageFormat: "Remove the first '{0}' method or use '{1}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DuplicateEnumerable_OrderBy));

    private static readonly DiagnosticDescriptor OptimizeCountRule = new(
        RuleIdentifiers.OptimizeEnumerable_Count,
        title: "Optimize Enumerable.Count() usage",
        messageFormat: "{0}",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_Count));

    private static readonly DiagnosticDescriptor OptimizeWhereAndOrderByRule = new(
        RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy,
        title: "Use Where before OrderBy",
        messageFormat: "Call 'Where' before '{0}'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_WhereBeforeOrderBy));

    private static readonly DiagnosticDescriptor UseCastInsteadOfSelect = new(
        RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect,
        title: "Use 'Cast' instead of 'Select' to cast",
        messageFormat: "Use 'Cast<{0}>' instead of 'Select' to cast",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_CastInsteadOfSelect));

    private static readonly DiagnosticDescriptor UseCountInsteadOfAny = new(
        RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny,
        title: "Use 'Count > 0' instead of 'Any()'",
        messageFormat: "Use 'Count > 0' instead of 'Any()'",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeEnumerable_UseCountInsteadOfAny));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ListMethodsRule,
        IndexerInsteadOfElementAtRule,
        CombineLinqMethodsRule,
        DuplicateOrderByMethodsRule,
        OptimizeCountRule,
        OptimizeWhereAndOrderByRule,
        UseCastInsteadOfSelect,
        UseCountInsteadOfAny);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.IsValid)
            {
                ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            EnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Linq.Enumerable");
            QueryableSymbol = compilation.GetBestTypeByMetadataName("System.Linq.Queryable");
            ExtensionMethodOwnerTypes.AddIfNotNull(EnumerableSymbol);
            ExtensionMethodOwnerTypes.AddIfNotNull(QueryableSymbol);

            ICollectionOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.ICollection`1");
            IReadOnlyCollectionOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1");
            ListOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1");
            IListOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IList`1");
            IReadOnlyListOfTSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
            ICollectionSymbol = compilation.GetBestTypeByMetadataName("System.Collections.ICollection");
        }

        public bool IsValid => ExtensionMethodOwnerTypes.Count > 0;

        private List<INamedTypeSymbol> ExtensionMethodOwnerTypes { get; } = [];

        private INamedTypeSymbol? EnumerableSymbol { get; set; }
        private INamedTypeSymbol? QueryableSymbol { get; set; }
        private INamedTypeSymbol? ICollectionOfTSymbol { get; set; }
        private INamedTypeSymbol? IReadOnlyCollectionOfTSymbol { get; set; }
        private INamedTypeSymbol? ListOfTSymbol { get; set; }
        private INamedTypeSymbol? IListOfTSymbol { get; set; }
        private INamedTypeSymbol? IReadOnlyListOfTSymbol { get; set; }
        private INamedTypeSymbol? ICollectionSymbol { get; set; }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Arguments.Length == 0)
                return;

            var method = operation.TargetMethod;
            if (!ExtensionMethodOwnerTypes.Contains(method.ContainingType))
                return;

            UseFindInsteadOfFirstOrDefault(context, operation);
            UseTrueForAllInsteadOfAll(context, operation);
            UseExistsInsteadOfAny(context, operation);
            UseCountPropertyInsteadOfMethod(context, operation);
            UseIndexerInsteadOfElementAt(context, operation);
            CombineWhereWithNextMethod(context, operation);
            RemoveTwoConsecutiveOrderBy(context, operation);
            WhereShouldBeBeforeOrderBy(context, operation);
            OptimizeCountUsage(context, operation);
            UseCastInsteadOfSelect(context, operation);
            UseCountInsteadOfAny(context, operation);
        }

        private static ImmutableDictionary<string, string?> CreateProperties(OptimizeLinqUsageData data)
        {
            return ImmutableDictionary.Create<string, string?>().Add("Data", data.ToString());
        }

        private void WhereShouldBeBeforeOrderBy(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name == nameof(Enumerable.OrderBy) ||
                operation.TargetMethod.Name == nameof(Enumerable.OrderByDescending))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent is not null && ExtensionMethodOwnerTypes.Contains(parent.TargetMethod.ContainingType))
                {
                    if (parent.TargetMethod.Name == nameof(Enumerable.Where))
                    {
                        var properties = CreateProperties(OptimizeLinqUsageData.CombineWhereWithNextMethod)
                           .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("MethodName", parent.TargetMethod.Name);

                        context.ReportDiagnostic(OptimizeWhereAndOrderByRule, properties, parent, operation.TargetMethod.Name);
                    }
                }
            }
        }

        private void UseCountPropertyInsteadOfMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.Arguments.Length != 1)
                return;

            if (operation.TargetMethod.Name == nameof(Enumerable.Count))
            {
                if (ICollectionOfTSymbol is null && IReadOnlyCollectionOfTSymbol is null)
                    return;

                var actualType = operation.Arguments[0].Value.GetActualType();
                if (actualType is null)
                    return;

                if (actualType.TypeKind == TypeKind.Array)
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.UseLengthProperty);
                    context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "Length", operation.TargetMethod.Name);
                    return;
                }

                if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.IsEqualTo(ICollectionOfTSymbol) || i.OriginalDefinition.IsEqualTo(IReadOnlyCollectionOfTSymbol)))
                {
                    // Ensure the Count property is not an explicit implementation
                    if (HasNonExplicitCountMethod(actualType))
                    {
                        var properties = CreateProperties(OptimizeLinqUsageData.UseCountProperty);
                        context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "Count", operation.TargetMethod.Name);
                        return;
                    }

                    static bool HasNonExplicitCountMethod(ITypeSymbol type)
                    {
                        foreach (var member in type.GetMembers("Count"))
                        {
                            if (member.Kind != SymbolKind.Property)
                                continue;

                            if (((IPropertySymbol)member).ExplicitInterfaceImplementations.Length == 0)
                                return true;
                        }

                        return false;
                    }
                }
            }
            else if (operation.TargetMethod.Name == nameof(Enumerable.LongCount))
            {
                var actualType = operation.Arguments[0].Value.GetActualType();
                if (actualType is not null && actualType.TypeKind == TypeKind.Array)
                {
                    var properties = CreateProperties(OptimizeLinqUsageData.UseLongLengthProperty);
                    context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "LongLength", operation.TargetMethod.Name);
                }
            }
        }

        private void UseFindInsteadOfFirstOrDefault(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name != nameof(Enumerable.FirstOrDefault))
                return;

            if (operation.Arguments.Length != 2)
                return;

            var firstArgumentType = operation.Arguments[0].Value.GetActualType();
            if (firstArgumentType is null)
                return;

            if (firstArgumentType.OriginalDefinition.IsEqualTo(ListOfTSymbol))
            {
                ImmutableDictionary<string, string?> properties;
                var predicateArgument = operation.Arguments[1].Value;
                if (predicateArgument is IDelegateCreationOperation)
                {
                    properties = CreateProperties(OptimizeLinqUsageData.UseFindMethod);
                }
                else
                {
                    if (!context.Options.GetConfigurationValue(operation, ListMethodsRule.Id + ".report_when_conversion_needed", defaultValue: false))
                        return;

                    properties = CreateProperties(OptimizeLinqUsageData.UseFindMethodWithConversion);
                }

                context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "Find()", operation.TargetMethod.Name);
            }
        }

        private void UseTrueForAllInsteadOfAll(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name != nameof(Enumerable.All))
                return;

            if (operation.Arguments.Length != 2)
                return;

            var firstArgumentType = operation.Arguments[0].Value.GetActualType();
            if (firstArgumentType is null)
                return;

            if (firstArgumentType.OriginalDefinition.IsEqualTo(ListOfTSymbol))
            {
                ImmutableDictionary<string, string?> properties;
                var predicateArgument = operation.Arguments[1].Value;
                if (predicateArgument is IDelegateCreationOperation)
                {
                    properties = CreateProperties(OptimizeLinqUsageData.UseTrueForAllMethod);
                }
                else
                {
                    if (!context.Options.GetConfigurationValue(operation, ListMethodsRule.Id + ".report_when_conversion_needed", defaultValue: false))
                        return;

                    properties = CreateProperties(OptimizeLinqUsageData.UseTrueForAllMethodWithConversion);
                }

                context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "TrueForAll()", operation.TargetMethod.Name);
            }
        }

        private void UseExistsInsteadOfAny(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name != nameof(Enumerable.Any))
                return;

            if (operation.Arguments.Length != 2)
                return;

            var firstArgumentType = operation.Arguments[0].Value.GetActualType();
            if (firstArgumentType is null)
                return;

            if (firstArgumentType.OriginalDefinition.IsEqualTo(ListOfTSymbol))
            {
                ImmutableDictionary<string, string?> properties;
                var predicateArgument = operation.Arguments[1].Value;
                if (predicateArgument is IDelegateCreationOperation)
                {
                    properties = CreateProperties(OptimizeLinqUsageData.UseExistsMethod);
                }
                else
                {
                    if (!context.Options.GetConfigurationValue(operation, ListMethodsRule.Id + ".report_when_conversion_needed", defaultValue: false))
                        return;

                    properties = CreateProperties(OptimizeLinqUsageData.UseExistsMethodWithConversion);
                }

                context.ReportDiagnostic(ListMethodsRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "Exists()", operation.TargetMethod.Name);
            }
        }

        private void UseIndexerInsteadOfElementAt(OperationAnalysisContext context, IInvocationOperation operation)
        {
            ImmutableDictionary<string, string?>? properties = null;

            var argCount = -1;
            if (operation.TargetMethod.Name == nameof(Enumerable.ElementAt))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexer);
                argCount = 2;
            }
            else if (operation.TargetMethod.Name == nameof(Enumerable.First))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexerFirst);
                argCount = 1;
            }
            else if (operation.TargetMethod.Name == nameof(Enumerable.Last))
            {
                properties = CreateProperties(OptimizeLinqUsageData.UseIndexerLast);
                argCount = 1;
            }

            if (argCount < 0)
                return;

            if (operation.Arguments.Length != argCount)
                return;

            if (IListOfTSymbol is null && IReadOnlyListOfTSymbol is null)
                return;

            var actualType = operation.Arguments[0].Value.GetActualType();
            if (actualType is null)
                return;

            if (actualType.AllInterfaces.Any(i => i.OriginalDefinition.IsEqualTo(IListOfTSymbol) || i.OriginalDefinition.IsEqualTo(IReadOnlyListOfTSymbol)))
            {
                context.ReportDiagnostic(IndexerInsteadOfElementAtRule, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, "[]", operation.TargetMethod.Name);
            }
        }

        private static readonly HashSet<string> CombinableLinqMethods = new(StringComparer.Ordinal)
        {
            nameof(Enumerable.First), nameof(Enumerable.FirstOrDefault),
            nameof(Enumerable.Last), nameof(Enumerable.LastOrDefault),
            nameof(Enumerable.Single), nameof(Enumerable.SingleOrDefault),
            nameof(Enumerable.Any),
            nameof(Enumerable.Count), nameof(Enumerable.LongCount),
            nameof(Enumerable.Where),
        };

        private void CombineWhereWithNextMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name == nameof(Enumerable.Where))
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
                if (parent is not null && ExtensionMethodOwnerTypes.Contains(parent.TargetMethod.ContainingType, SymbolEqualityComparer.Default))
                {
                    if (CombinableLinqMethods.Contains(parent.TargetMethod.Name))
                    {
                        var properties = CreateProperties(OptimizeLinqUsageData.CombineWhereWithNextMethod)
                           .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                           .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                           .Add("MethodName", parent.TargetMethod.Name);

                        context.ReportDiagnostic(CombineLinqMethodsRule, properties, parent, operation.TargetMethod.Name, parent.TargetMethod.Name);
                    }
                }
            }
        }

        private void RemoveTwoConsecutiveOrderBy(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name == nameof(Enumerable.OrderBy) ||
                operation.TargetMethod.Name == nameof(Enumerable.OrderByDescending) ||
                operation.TargetMethod.Name == nameof(Enumerable.ThenBy) ||
                operation.TargetMethod.Name == nameof(Enumerable.ThenByDescending))
            {
                var parent = GetParentLinqOperation(operation);
                if (parent is not null && ExtensionMethodOwnerTypes.Contains(parent.TargetMethod.ContainingType, SymbolEqualityComparer.Default))
                {
                    if (parent.TargetMethod.Name == nameof(Enumerable.OrderBy) ||
                        parent.TargetMethod.Name == nameof(Enumerable.OrderByDescending))
                    {
                        var expectedMethodName = parent.TargetMethod.Name.Replace("OrderBy", "ThenBy", StringComparison.Ordinal);
                        var properties = CreateProperties(OptimizeLinqUsageData.DuplicatedOrderBy)
                            .Add("FirstOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                            .Add("FirstOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                            .Add("LastOperationStart", parent.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                            .Add("LastOperationLength", parent.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                            .Add("ExpectedMethodName", expectedMethodName)
                            .Add("MethodName", parent.TargetMethod.Name);

                        context.ReportDiagnostic(DuplicateOrderByMethodsRule, properties, parent, operation.TargetMethod.Name, expectedMethodName);
                    }
                }
            }
        }

        private void OptimizeCountUsage(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name != nameof(Enumerable.Count))
                return;

            var binaryOperation = GetParentBinaryOperation(operation, out var countOperand);
            if (binaryOperation is null)
                return;

            if (!IsSupportedOperator(binaryOperation.OperatorKind))
                return;

            if (!binaryOperation.LeftOperand.Type.IsInt32() || !binaryOperation.RightOperand.Type.IsInt32())
                return;

            var opKind = NormalizeOperator();
            var otherOperand = binaryOperation.LeftOperand == countOperand ? binaryOperation.RightOperand : binaryOperation.LeftOperand;
            if (otherOperand is null)
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
                            if (!HasTake(operation, ExtensionMethodOwnerTypes))
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
                            if (!HasTake(operation, ExtensionMethodOwnerTypes))
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
                        if (!HasTake(operation, ExtensionMethodOwnerTypes))
                        {
                            message = "Replace 'Count() == n' with 'Take(n + 1).Count() == n'";
                            properties = CreateProperties(OptimizeLinqUsageData.UseTakeAndCount);
                        }

                        break;

                    case BinaryOperatorKind.NotEquals:
                        // expr.Count() != 1
                        if (!HasTake(operation, ExtensionMethodOwnerTypes))
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

            if (message is not null)
            {
                properties = properties
                       .Add("OperandOperationStart", otherOperand.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("OperandOperationLength", otherOperand.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture))
                       .Add("CountOperationStart", operation.Syntax.Span.Start.ToString(CultureInfo.InvariantCulture))
                       .Add("CountOperationLength", operation.Syntax.Span.Length.ToString(CultureInfo.InvariantCulture));

                context.ReportDiagnostic(OptimizeCountRule, properties, binaryOperation, message);
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

            static bool HasTake(IInvocationOperation operation, List<INamedTypeSymbol> extensionMethodOwnerTypes)
            {
                var op = GetChildLinqOperation(operation);
                if (op is null)
                    return false;

                return op.TargetMethod.Name == nameof(Enumerable.Take) && extensionMethodOwnerTypes.Contains(op.TargetMethod.ContainingType, SymbolEqualityComparer.Default);
            }
        }

        private static void UseCastInsteadOfSelect(OperationAnalysisContext context, IInvocationOperation operation)
        {
            var semanticModel = operation.SemanticModel!;

            if (operation.TargetMethod.Name != nameof(Enumerable.Select))
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
            if (returnOp.ReturnedValue is not IConversionOperation castOp || castOp.IsTryCast || castOp.Type is null)
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
            var selectMethodSymbol = semanticModel.GetSymbolInfo(operation.Syntax, context.CancellationToken).Symbol as IMethodSymbol;
            var nullableFlowState = selectMethodSymbol?.TypeArgumentNullableAnnotations[1] == NullableAnnotation.Annotated ?
                NullableFlowState.MaybeNull :
                NullableFlowState.None;

            var typeSyntax = castOp.Syntax;
            if (typeSyntax is CastExpressionSyntax castSyntax)
            {
                typeSyntax = castSyntax.Type;
            }

            // Get the cast type's minimally qualified name, in the current context
            var properties = CreateProperties(OptimizeLinqUsageData.UseCastInsteadOfSelect);

            var castType = castOp.Type.ToMinimalDisplayString(semanticModel, nullableFlowState, operation.Syntax.SpanStart);
            context.ReportDiagnostic(OptimizeLinqUsageAnalyzer.UseCastInsteadOfSelect, properties, operation, DiagnosticInvocationReportOptions.ReportOnMember, castType);

            static bool CanReplaceByCast(IConversionOperation op)
            {
                if (op.Conversion.IsUserDefined || op.Conversion.IsNumeric)
                    return false;

                // Handle enums: source.Select<MyEnum, byte>(item => (byte)item);
                // Using Cast<T> is only possible when the enum underlying type is the same as the conversion type
                var operandActualType = op.Operand.GetActualType();
                var enumerationType = operandActualType.GetEnumerationType();
                if (enumerationType is not null)
                {
                    if (!enumerationType.IsEqualTo(op.Type))
                        return false;
                }

                return true;
            }
        }

        private void UseCountInsteadOfAny(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name == "Any" && operation.TargetMethod.ContainingType.IsEqualTo(EnumerableSymbol))
            {
                // Any(_ => true)
                if (operation.Arguments.Length >= 2)
                    return;

                var operandType = operation.Arguments[0].Value.GetActualType();
                if (operandType is null)
                    return;

                var implementedInterfaces = operandType.GetAllInterfacesIncludingThis().Select(i => i.OriginalDefinition);
                if (implementedInterfaces.Any(i => i.IsEqualTo(ICollectionOfTSymbol) || i.IsEqualTo(ICollectionSymbol) || i.IsEqualTo(IReadOnlyCollectionOfTSymbol)))
                {
                    context.ReportDiagnostic(OptimizeLinqUsageAnalyzer.UseCountInsteadOfAny, operation);
                }
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
}
