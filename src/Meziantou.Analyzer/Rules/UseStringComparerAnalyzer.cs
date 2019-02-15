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
    public class UseStringComparerAnalyzer : DiagnosticAnalyzer
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

        private static readonly Dictionary<string, int> s_arityIndex = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "GroupBy", 1 },
            { "GroupJoin", 2 },
            { "Join", 2 },
            { "ToDictionary", 1 },
            { "ToLookup", 1 },
        };

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseStringComparer,
            title: "IEqualityComparer<string> is missing",
            messageFormat: "Use an overload that has a IEqualityComparer<string> parameter",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparer));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(AnalyzeConstructors);
            context.RegisterCompilationStartAction(AnalyzeMethods);
        }

        private INamedTypeSymbol GetIComparableString(Compilation compilation)
        {
            var equalityComparerInterfaceType = compilation.GetTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
            if (equalityComparerInterfaceType == null)
                return null;

            var stringType = compilation.GetSpecialType(SpecialType.System_String);
            if (stringType == null)
                return null;

            return equalityComparerInterfaceType.Construct(stringType);
        }

        private void AnalyzeConstructors(CompilationStartAnalysisContext compilationContext)
        {
            var stringEqualityComparerInterfaceType = GetIComparableString(compilationContext.Compilation);
            if (stringEqualityComparerInterfaceType == null)
                return;

            var types = new List<INamedTypeSymbol>();
            types.AddIfNotNull(compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1"));
            types.AddIfNotNull(compilationContext.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2"));
            types.AddIfNotNull(compilationContext.Compilation.GetTypesByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2"));

            if (types.Count > 0)
            {
                compilationContext.RegisterOperationAction(operationContext =>
                {
                    var operation = (IObjectCreationOperation)operationContext.Operation;
                    var type = operation.Type as INamedTypeSymbol;
                    if (type == null || type.OriginalDefinition == null)
                        return;

                    if (types.Any(t => type.OriginalDefinition.Equals(t)))
                    {
                        // We only care about dictionaries that use a string as the key
                        if (!type.TypeArguments[0].IsString())
                            return;

                        if (!HasEqualityComparerArgument(stringEqualityComparerInterfaceType, operation.Arguments))
                        {
                            operationContext.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
                        }
                    }
                }, OperationKind.ObjectCreation);
            }
        }

        private void AnalyzeMethods(CompilationStartAnalysisContext compilationContext)
        {
            var stringEqualityComparerInterfaceType = GetIComparableString(compilationContext.Compilation);
            if (stringEqualityComparerInterfaceType == null)
                return;

            var enumerableType = compilationContext.Compilation.GetTypeByMetadataName("System.Linq.Enumerable");
            if (enumerableType == null)
                return;

            compilationContext.RegisterOperationAction(operationContext =>
            {
                var operation = (IInvocationOperation)operationContext.Operation;
                if (operation == null)
                    return;

                var method = operation.TargetMethod;
                if (!method.ContainingType.IsEqualsTo(enumerableType))
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

                if (!HasEqualityComparerArgument(stringEqualityComparerInterfaceType, operation.Arguments))
                {
                    operationContext.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
                }
            }, OperationKind.Invocation);
        }

        private static bool HasEqualityComparerArgument(INamedTypeSymbol stringEqualityComparerInterfaceType, ImmutableArray<IArgumentOperation> arguments)
        {
            var hasEqualityComparer = false;
            foreach (var argument in arguments)
            {
                var argumentType = argument.Value.Type;
                if (argumentType == null)
                    continue;

                if (argumentType.GetAllInterfacesIncludingThis().Any(i => stringEqualityComparerInterfaceType.Equals(i)))
                {
                    hasEqualityComparer = true;
                    break;
                }
            }

            return hasEqualityComparer;
        }
    }
}
