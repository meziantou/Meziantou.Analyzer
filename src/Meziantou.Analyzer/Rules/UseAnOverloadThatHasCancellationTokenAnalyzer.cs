using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAnOverloadThatHasCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor UseAnOverloadThatHasCancellationTokenRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationToken,
        title: "Use an overload with a CancellationToken argument",
        messageFormat: "Use an overload with a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationToken));

    private static readonly DiagnosticDescriptor UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken parameter to methods that take one",
        messageFormat: "Use an overload with a CancellationToken, available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable));

    private static readonly DiagnosticDescriptor FlowCancellationTokenInAwaitForEachRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEach,
        title: "Use a cancellation token using .WithCancellation()",
        messageFormat: "Specify a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEach));

    private static readonly DiagnosticDescriptor FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken using .WithCancellation()",
        messageFormat: "Specify a CancellationToken using WithCancellation(), available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(UseAnOverloadThatHasCancellationTokenRule, UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, FlowCancellationTokenInAwaitForEachRule, FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.CancellationTokenSymbol is null)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeLoop, OperationKind.Loop);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentDictionary<(ITypeSymbol Symbol, int MaxDepth), List<ISymbol[]>?> _membersByType = new();

        private readonly OverloadFinder _overloadFinder = new(compilation);

        public INamedTypeSymbol CancellationTokenSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken")!;  // Not nullable as it is checked before registering the Operation actions
        public INamedTypeSymbol? CancellationTokenSourceSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.CancellationTokenSource");
        private INamedTypeSymbol? TaskSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        private INamedTypeSymbol? TaskOfTSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
        private INamedTypeSymbol? ConfiguredCancelableAsyncEnumerableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1");
        private INamedTypeSymbol? EnumeratorCancellationAttributeSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.EnumeratorCancellationAttribute");
        private INamedTypeSymbol? XunitTestContextSymbol { get; } = compilation.GetBestTypeByMetadataName("Xunit.TestContext");

        private bool HasExplicitCancellationTokenArgument(IInvocationOperation operation)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind == ArgumentKind.Explicit && argument.Parameter is not null && argument.Parameter.Type.IsEqualTo(CancellationTokenSymbol))
                    return true;
            }

            return false;
        }

        private sealed record AdditionalParameterInfo(int ParameterIndex, string? Name, bool HasEnumeratorCancellationAttribute);

        private bool HasAnOverloadWithCancellationToken(OperationAnalysisContext context, IInvocationOperation operation, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
        {
            parameterInfo = default;
            var method = operation.TargetMethod;
            if (method.Name == nameof(CancellationTokenSource.CreateLinkedTokenSource) && method.ContainingType.IsEqualTo(CancellationTokenSourceSymbol))
                return false;

            if (IsArgumentImplicitlyDeclared(operation, CancellationTokenSymbol, out parameterInfo))
                return true;

            var allowOptionalParameters = context.Options.GetConfigurationValue(operation, "MA0032.allowOverloadsWithOptionalParameters", defaultValue: false);
            var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, operation, includeObsoleteMethods: false, allowOptionalParameters, CancellationTokenSymbol);
            if (overload is not null)
            {
                for (var i = 0; i < overload.Parameters.Length; i++)
                {
                    if (overload.Parameters[i].Type.IsEqualTo(CancellationTokenSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(i, overload.Parameters[i].Name, HasEnumerableCancellationAttribute(overload.Parameters[i]));
                        break;
                    }
                }

                Debug.Assert(parameterInfo != null);
                return true;
            }

            return false;

            bool IsArgumentImplicitlyDeclared(IInvocationOperation invocationOperation, INamedTypeSymbol cancellationTokenSymbol, [NotNullWhen(true)] out AdditionalParameterInfo? parameterInfo)
            {
                foreach (var arg in invocationOperation.Arguments)
                {
                    if (arg.IsImplicit && arg.Parameter is not null && arg.Parameter.Type.IsEqualTo(cancellationTokenSymbol))
                    {
                        parameterInfo = new AdditionalParameterInfo(invocationOperation.TargetMethod.Parameters.IndexOf(arg.Parameter), arg.Parameter.Name, HasEnumerableCancellationAttribute(arg.Parameter));
                        return true;
                    }
                }

                parameterInfo = null;
                return false;
            }
        }

        private bool HasEnumerableCancellationAttribute(IParameterSymbol? parameterSymbol)
        {
            if (parameterSymbol is null)
                return false;

            return parameterSymbol.HasAttribute(EnumeratorCancellationAttributeSymbol, inherits: false);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (HasExplicitCancellationTokenArgument(operation))
                return;

            if (!HasAnOverloadWithCancellationToken(context, operation, out var parameterInfo))
                return;

            var availableCancellationTokens = FindCancellationTokens(operation, context.CancellationToken);
            if (availableCancellationTokens.Length > 0)
            {
                context.ReportDiagnostic(UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, CreateProperties(availableCancellationTokens, parameterInfo), operation, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = operation.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(UseAnOverloadThatHasCancellationTokenRule, CreateProperties(availableCancellationTokens, parameterInfo), operation);
            }
        }

        public void AnalyzeLoop(OperationAnalysisContext context)
        {
            if (context.Operation is not IForEachLoopOperation op)
                return;

            if (!op.IsAsynchronous)
                return;

            var collectionType = op.Collection.GetActualType();
            if (collectionType.IsEqualTo(ConfiguredCancelableAsyncEnumerableSymbol))
                return;

            // await foreach (var item in A(cancellationToken)) OK
            // await foreach (var item in A())                  KO
            // await foreach (var item in a)                    KO
            var collection = op.Collection;
            if (collection is IConversionOperation conversion)
            {
                collection = conversion.Operand;
            }

            while (collection is IInvocationOperation invocation)
            {
                if (HasExplicitCancellationTokenArgument(invocation))
                    return;

                // Already handled by AnalyzeInvocation
                if (HasAnOverloadWithCancellationToken(context, invocation, out _))
                    return;

                collection = invocation.GetChildOperations().FirstOrDefault();
                if (collection is IArgumentOperation argOperation)
                {
                    collection = argOperation.Value;
                }
            }

            var availableCancellationTokens = FindCancellationTokens(op, context.CancellationToken);
            if (availableCancellationTokens.Length > 0)
            {
                var properties = CreateProperties(availableCancellationTokens, new AdditionalParameterInfo(-1, Name: null, HasEnumeratorCancellationAttribute: false));
                context.ReportDiagnostic(FlowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule, properties, op.Collection, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = op.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(FlowCancellationTokenInAwaitForEachRule, op.Collection, string.Join(", ", availableCancellationTokens));
            }
        }

        private static ImmutableDictionary<string, string?> CreateProperties(string[] cancellationTokens, AdditionalParameterInfo parameterInfo)
        {
            return ImmutableDictionary.Create<string, string?>()
                .Add("ParameterIndex", parameterInfo.ParameterIndex.ToString(CultureInfo.InvariantCulture))
                .Add("ParameterName", parameterInfo.Name)
                .Add("ParameterIsEnumeratorCancellation", parameterInfo.HasEnumeratorCancellationAttribute.ToString())
                .Add("CancellationTokens", string.Join(',', cancellationTokens));
        }

        private List<ISymbol[]>? GetMembers(ITypeSymbol symbol, int maxDepth)
        {
            return _membersByType.GetOrAdd((symbol, maxDepth), item =>
            {
                var (symbol, maxDepth) = item;

                if (maxDepth < 0)
                    return null;

                // quickly skips some basic types that are known to not contain CancellationToken
                if ((int)symbol.SpecialType is >= 1 and <= 45)
                    return null;

                if (symbol.IsEqualTo(TaskSymbol) || symbol.OriginalDefinition.IsEqualTo(TaskOfTSymbol))
                    return null;

                if (symbol.IsEqualTo(CancellationTokenSymbol))
                    return [[]];

                var result = new List<ISymbol[]>();
                var members = symbol.GetAllMembers();
                foreach (var member in members)
                {
                    if (member.IsImplicitlyDeclared)
                        continue;

                    ITypeSymbol memberTypeSymbol;
                    switch (member)
                    {
                        case IPropertySymbol propertySymbol:
                            memberTypeSymbol = propertySymbol.Type;
                            break;

                        case IFieldSymbol fieldSymbol:
                            memberTypeSymbol = fieldSymbol.Type;
                            break;

                        default:
                            continue;
                    }

                    if (memberTypeSymbol.IsEqualTo(CancellationTokenSymbol))
                    {
                        result.Add([member]);
                    }
                    else
                    {
                        var typeMembers = GetMembers(memberTypeSymbol, maxDepth - 1);
                        if (typeMembers is not null)
                        {
                            foreach (var objectMember in typeMembers)
                            {
                                result.Add([.. Prepend(member, objectMember)]);
                            }
                        }
                    }
                }

                return result;
            });
        }

        private string[] FindCancellationTokens(IOperation operation, CancellationToken cancellationToken)
        {
            var availableSymbols = new List<NameAndType>();

            foreach (var symbol in operation.LookupAvailableSymbols(cancellationToken))
            {
                if (symbol is IMethodSymbol)
                    continue;

                var symbolType = symbol.GetSymbolType();
                if (symbolType is null)
                    continue;

                availableSymbols.Add(new(symbol.Name, symbolType));
            }

            if (availableSymbols.Count == 0 && XunitTestContextSymbol is null)
                return [];

            var isInStaticContext = operation.IsInStaticContext(cancellationToken);

            // For each symbol, get their members
            var paths = new List<string>();
            foreach (var availableSymbol in availableSymbols)
            {
                if (availableSymbol.TypeSymbol is null)
                    continue;

                var members = GetMembers(availableSymbol.TypeSymbol, maxDepth: 1);
                if (members is not null)
                {
                    foreach (var member in members)
                    {
                        if (!AreAllSymbolsAccessibleFromOperation(member, operation))
                            continue;

                        if (availableSymbol.Name is null && isInStaticContext && member.Length > 0 && !member[0].IsStatic)
                            continue;

                        var fullPath = ComputeFullPath(availableSymbol.Name, member);
                        paths.Add(fullPath);
                    }
                }
            }

            if (XunitTestContextSymbol != null)
            {
                paths.Add("Xunit.TestContext.Current.CancellationToken");
            }

            if (paths.Count == 0)
                return [];

            return [.. paths.OrderBy(value => value.Count(c => c == '.')).ThenBy(value => value, StringComparer.Ordinal)];

            static bool AreAllSymbolsAccessibleFromOperation(IEnumerable<ISymbol> symbols, IOperation operation)
            {
                foreach (var item in symbols)
                {
                    if (!IsSymbolAccessibleFromOperation(item, operation))
                        return false;
                }

                return true;
            }

            static string ComputeFullPath(string? prefix, IEnumerable<ISymbol> symbols)
            {
                if (prefix is null)
                    return string.Join('.', symbols.Select(symbol => symbol.Name));

                var suffix = string.Join('.', symbols.Select(symbol => symbol.Name));
                if (string.IsNullOrEmpty(suffix))
                    return prefix;

                return prefix + "." + suffix;
            }

            static bool IsSymbolAccessibleFromOperation(ISymbol symbol, IOperation operation)
            {
                return operation.SemanticModel!.IsAccessible(operation.Syntax.Span.Start, symbol);
            }
        }

        private static ITypeSymbol? GetContainingType(IOperation operation, CancellationToken cancellationToken)
        {
            var ancestor = operation.Syntax.Ancestors().FirstOrDefault(node => node is TypeDeclarationSyntax);
            if (ancestor is null)
                return null;

            return operation.SemanticModel!.GetDeclaredSymbol(ancestor, cancellationToken) as ITypeSymbol;
        }

        private static IEnumerable<T> Prepend<T>(T value, IEnumerable<T> items)
        {
            yield return value;
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct NameAndType
    {
        public NameAndType(string? name, ITypeSymbol? typeSymbol)
        {
            Name = name;
            TypeSymbol = typeSymbol;
        }

        public string? Name { get; }
        public ITypeSymbol? TypeSymbol { get; }
    }
}
