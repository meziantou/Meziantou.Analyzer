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
    public sealed class UseStringComparerAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string[] s_enumerableMethods =
        {
            "Contains",
            "Distinct",
            "Except",
            "Intersect",
            "OrderBy",
            "OrderByDescending",
            "SequenceEqual",
            "ToHashSet",
            "Union",
        };

        private static readonly Dictionary<string, int> s_arityIndex = new(StringComparer.Ordinal)
        {
            { "GroupBy", 1 },
            { "GroupJoin", 2 },
            { "Join", 2 },
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

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                EqualityComparerStringType = GetIEqualityComparerString(compilation);
                ComparerStringType = GetIComparerString(compilation);
                EnumerableType = compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            }

            public INamedTypeSymbol? EqualityComparerStringType { get; }
            public INamedTypeSymbol? ComparerStringType { get; }
            public INamedTypeSymbol? EnumerableType { get; }

            public void AnalyzeConstructor(OperationAnalysisContext ctx)
            {
                var operation = (IObjectCreationOperation)ctx.Operation;
                if (HasEqualityComparerArgument(operation.Arguments))
                    return;

                var method = operation.Constructor;
                if (method.HasOverloadWithAdditionalParameterOfType(ctx.Compilation, EqualityComparerStringType) ||
                    method.HasOverloadWithAdditionalParameterOfType(ctx.Compilation, ComparerStringType))
                {
                    ctx.ReportDiagnostic(s_rule, operation);
                }
            }

            public void AnalyzeInvocation(OperationAnalysisContext ctx)
            {
                var operation = (IInvocationOperation)ctx.Operation;
                if (HasEqualityComparerArgument(operation.Arguments))
                    return;

                var method = operation.TargetMethod;
                if (method.HasOverloadWithAdditionalParameterOfType(ctx.Compilation, EqualityComparerStringType) ||
                    method.HasOverloadWithAdditionalParameterOfType(ctx.Compilation, ComparerStringType))
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
                var equalityComparerInterfaceType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
                if (equalityComparerInterfaceType == null)
                    return null;

                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                if (stringType == null)
                    return null;

                return equalityComparerInterfaceType.Construct(stringType);
            }

            private static INamedTypeSymbol? GetIComparerString(Compilation compilation)
            {
                var equalityComparerInterfaceType = compilation.GetTypeByMetadataName("System.Collections.Generic.IComparer`1");
                if (equalityComparerInterfaceType == null)
                    return null;

                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                if (stringType == null)
                    return null;

                return equalityComparerInterfaceType.Construct(stringType);
            }
        }
    }
}
