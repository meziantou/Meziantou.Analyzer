using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringComparerAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] s_enumerableMethods =
    [
        "Contains",
        "Distinct",
        "Except",
        "Intersect",
        "Order",
        "OrderBy",
        "OrderByDescending",
        "SequenceEqual",
        "ThenBy",
        "ThenByDescending",
        "ToHashSet",
        "Union",
    ];

    private static readonly Dictionary<string, int> s_arityIndex = new(StringComparer.Ordinal)
    {
        { "GroupBy", 1 },
        { "GroupJoin", 2 },
        { "Join", 2 },
        { "OrderBy", 1 },
        { "OrderByDescending", 1 },
        { "ThenBy", 1 },
        { "ThenByDescending", 1 },
        { "ToDictionary", 1 },
        { "ToLookup", 1 },
    };

    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseStringComparer,
        title: "IEqualityComparer<string> or IComparer<string> is missing",
        messageFormat: "Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeConstructor, OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly OperationUtilities _operationUtilities = new(compilation);

        public INamedTypeSymbol? EqualityComparerStringType { get; } = GetIEqualityComparerString(compilation);
        public INamedTypeSymbol? ComparerStringType { get; } = GetIComparerString(compilation);
        public INamedTypeSymbol? EnumerableType { get; } = compilation.GetBestTypeByMetadataName("System.Linq.Enumerable");
        public INamedTypeSymbol? ISetType { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.ISet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));

        public void AnalyzeConstructor(OperationAnalysisContext ctx)
        {
            var operation = (IObjectCreationOperation)ctx.Operation;
            if (HasEqualityComparerArgument(operation.Arguments))
                return;

            var method = operation.Constructor;
            if (method == null)
                return;

            if ((EqualityComparerStringType != null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, EqualityComparerStringType)) ||
                (ComparerStringType != null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, ComparerStringType)))
            {
                ctx.ReportDiagnostic(s_rule, operation);
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext ctx)
        {
            var operation = (IInvocationOperation)ctx.Operation;
            if (HasEqualityComparerArgument(operation.Arguments))
                return;

            if (_operationUtilities.IsInExpressionContext(operation))
                return;

            var method = operation.TargetMethod;

            // Most ISet implementation already configured the IEqualityComparer in this constructor,
            // so it should be ok to skip method calls on those types.
            // A concrete use-case is HashSet<string>.Contains which has an extension method IEnumerable.Contains(value, comparer)
            if (ISetType != null && method.ContainingType.IsOrImplements(ISetType))
                return;

            if (operation.Instance != null && operation.Instance.GetActualType()?.IsOrImplements(ISetType) == true)
                return;

            if (operation.IsImplicit && IsQueryOperator(operation) && ctx.Options.GetConfigurationValue(operation, s_rule.Id + ".exclude_query_operator_syntaxes", defaultValue: false))
                return;

            if ((EqualityComparerStringType != null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, operation, EqualityComparerStringType)) ||
                (ComparerStringType != null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, operation, ComparerStringType)))
            {
                ctx.ReportDiagnostic(s_rule, operation);
                return;
            }

            if (EnumerableType != null)
            {
                if (!method.ContainingType.IsEqualTo(EnumerableType))
                    return;

                if (method.Arity == 0)
                    return;

                if (method.Arity == 1)
                {
                    if (!s_enumerableMethods.Contains(method.Name, StringComparer.Ordinal))
                        return;

                    if (!method.TypeArguments[0].IsString())
                        return;
                }
                else
                {
                    if (!s_arityIndex.TryGetValue(method.Name, out var arityIndex))
                        return;

                    if (arityIndex >= method.Arity)
                        return;

                    if (!method.TypeArguments[arityIndex].IsString())
                        return;
                }

                if (!HasEqualityComparerArgument(operation.Arguments))
                {
                    ctx.ReportDiagnostic(s_rule, operation);
                }
            }
        }

        private static bool IsQueryOperator(IOperation operation)
        {
            var syntax = operation.Syntax;
            return syntax.IsKind(SyntaxKind.SelectClause)
                || syntax.IsKind(SyntaxKind.GroupClause)
                || syntax.IsKind(SyntaxKind.OrderByClause)
                || syntax.IsKind(SyntaxKind.AscendingOrdering)
                || syntax.IsKind(SyntaxKind.DescendingOrdering)
                || syntax.IsKind(SyntaxKind.JoinClause)
                || syntax.IsKind(SyntaxKind.JoinIntoClause);
        }

        private bool HasEqualityComparerArgument(ImmutableArray<IArgumentOperation> arguments)
        {
            foreach (var argument in arguments)
            {
                var argumentType = argument.Value.Type;
                if (argumentType == null)
                    continue;

                if (argumentType.GetAllInterfacesIncludingThis().Any(i => EqualityComparerStringType.IsEqualTo(i) || ComparerStringType.IsEqualTo(i)))
                    return true;
            }

            return false;
        }

        private static INamedTypeSymbol? GetIEqualityComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
            if (equalityComparerInterfaceType == null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }

        private static INamedTypeSymbol? GetIComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IComparer`1");
            if (equalityComparerInterfaceType == null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }
    }
}
