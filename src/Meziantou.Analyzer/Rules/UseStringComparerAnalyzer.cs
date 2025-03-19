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
    private const DiagnosticInvocationReportOptions DefaultDiagnosticInvocationReportOptions = DiagnosticInvocationReportOptions.ReportOnMember | DiagnosticInvocationReportOptions.ReportOnArguments;

    private static readonly string[] EnumerableMethods =
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

    private static readonly Dictionary<string, int> ArityIndex = new(StringComparer.Ordinal)
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

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStringComparer,
        title: "IEqualityComparer<string> or IComparer<string> is missing",
        messageFormat: "Use an overload that has a IEqualityComparer<string> or IComparer<string> parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparer));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
        public INamedTypeSymbol? IReadOnlySetType { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IReadOnlySet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));
        public INamedTypeSymbol? IImmutableSetType { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Immutable.IImmutableSet`1")?.Construct(compilation.GetSpecialType(SpecialType.System_String));

        public void AnalyzeConstructor(OperationAnalysisContext ctx)
        {
            var operation = (IObjectCreationOperation)ctx.Operation;
            if (HasEqualityComparerArgument(operation.Arguments))
                return;

            var method = operation.Constructor;
            if (method is null)
                return;

            if ((EqualityComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, EqualityComparerStringType)) ||
                (ComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, ComparerStringType)))
            {
                ctx.ReportDiagnostic(Rule, operation);
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
            foreach (var type in (ReadOnlySpan<ITypeSymbol?>)[ISetType, IReadOnlySetType, IImmutableSetType])
            {

                if (type is null)
                    continue;

                if (method.ContainingType.IsOrImplements(type))
                    return;

                if (operation.Instance is not null && operation.Instance.GetActualType()?.IsOrImplements(type) is true)
                    return;
            }

            if (operation.IsImplicit && IsQueryOperator(operation) && ctx.Options.GetConfigurationValue(operation, Rule.Id + ".exclude_query_operator_syntaxes", defaultValue: false))
                return;

            if ((EqualityComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, operation, EqualityComparerStringType)) ||
                (ComparerStringType is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(method, operation, ComparerStringType)))
            {
                ctx.ReportDiagnostic(Rule, operation, DefaultDiagnosticInvocationReportOptions);
                return;
            }

            if (EnumerableType is not null)
            {
                if (!method.ContainingType.IsEqualTo(EnumerableType))
                    return;

                if (method.Arity == 0)
                    return;

                if (method.Arity == 1)
                {
                    if (!EnumerableMethods.Contains(method.Name, StringComparer.Ordinal))
                        return;

                    if (!method.TypeArguments[0].IsString())
                        return;
                }
                else
                {
                    if (!ArityIndex.TryGetValue(method.Name, out var arityIndex))
                        return;

                    if (arityIndex >= method.Arity)
                        return;

                    if (!method.TypeArguments[arityIndex].IsString())
                        return;
                }

                if (!HasEqualityComparerArgument(operation.Arguments))
                {
                    ctx.ReportDiagnostic(Rule, operation, DefaultDiagnosticInvocationReportOptions);
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
                if (argumentType is null)
                    continue;

                if (argumentType.GetAllInterfacesIncludingThis().Any(i => EqualityComparerStringType.IsEqualTo(i) || ComparerStringType.IsEqualTo(i)))
                    return true;
            }

            return false;
        }

        private static INamedTypeSymbol? GetIEqualityComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
            if (equalityComparerInterfaceType is null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType is null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }

        private static INamedTypeSymbol? GetIComparerString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IComparer`1");
            if (equalityComparerInterfaceType is null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType is null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }
    }
}
