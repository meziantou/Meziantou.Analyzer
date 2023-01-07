using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseAnOverloadThatHasCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_useAnOverloadThatHasCancellationTokenRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationToken,
        title: "Use an overload with a CancellationToken argument",
        messageFormat: "Use an overload with a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationToken));

    private static readonly DiagnosticDescriptor s_useAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken parameter to methods that take one",
        messageFormat: "Use an overload with a CancellationToken, available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailable));

    private static readonly DiagnosticDescriptor s_flowCancellationTokenInAwaitForEachRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEach,
        title: "Use a cancellation token using .WithCancellation()",
        messageFormat: "Specify a CancellationToken",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEach));

    private static readonly DiagnosticDescriptor s_flowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule = new(
        RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable,
        title: "Forward the CancellationToken using .WithCancellation()",
        messageFormat: "Specify a CancellationToken using WithCancellation(), available tokens: {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FlowCancellationTokenInAwaitForEachWhenACancellationTokenIsAvailable));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_useAnOverloadThatHasCancellationTokenRule, s_useAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, s_flowCancellationTokenInAwaitForEachRule, s_flowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            if (analyzerContext.CancellationTokenSymbol == null)
                return;

            ctx.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeLoop, OperationKind.Loop);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly ConcurrentDictionary<(ITypeSymbol Symbol, int MaxDepth), List<IReadOnlyList<ISymbol>>?> _membersByType = new();

        public AnalyzerContext(Compilation compilation)
        {
            Compilation = compilation;
            CancellationTokenSymbol = compilation.GetBestTypeByMetadataName("System.Threading.CancellationToken")!;  // Not nullable as it is checked before registering the Operation actions
            CancellationTokenSourceSymbol = compilation.GetBestTypeByMetadataName("System.Threading.CancellationTokenSource");
            TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
            ConfiguredCancelableAsyncEnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1");
        }

        public Compilation Compilation { get; }
        public INamedTypeSymbol CancellationTokenSymbol { get; }
        public INamedTypeSymbol? CancellationTokenSourceSymbol { get; }
        private INamedTypeSymbol? TaskSymbol { get; }
        private INamedTypeSymbol? TaskOfTSymbol { get; }
        private INamedTypeSymbol? ConfiguredCancelableAsyncEnumerableSymbol { get; }

        private bool HasExplicitCancellationTokenArgument(IInvocationOperation operation)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind == ArgumentKind.Explicit && argument.Parameter != null && argument.Parameter.Type.IsEqualTo(CancellationTokenSymbol))
                    return true;
            }

            return false;
        }

        private bool HasAnOverloadWithCancellationToken(IInvocationOperation operation, out int parameterIndex, out string? parameterName)
        {
            parameterName = null;
            parameterIndex = -1;
            var method = operation.TargetMethod;
            if (method.Name == nameof(CancellationTokenSource.CreateLinkedTokenSource) && method.ContainingType.IsEqualTo(CancellationTokenSourceSymbol))
                return false;

            if (IsArgumentImplicitlyDeclared(operation, CancellationTokenSymbol, out parameterIndex, out parameterName))
                return true;

            var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(operation, includeObsoleteMethods: false, CancellationTokenSymbol);
            if (overload != null)
            {
                for (var i = 0; i < overload.Parameters.Length; i++)
                {
                    if (overload.Parameters[i].Type.IsEqualTo(CancellationTokenSymbol))
                    {
                        parameterName ??= overload.Parameters[i].Name;
                        parameterIndex = i;
                        break;
                    }
                }

                return true;
            }


            return false;

            static bool IsArgumentImplicitlyDeclared(IInvocationOperation invocationOperation, INamedTypeSymbol cancellationTokenSymbol, out int parameterIndex, [NotNullWhen(true)] out string? parameterName)
            {
                parameterIndex = -1;
                parameterName = null;
                foreach (var arg in invocationOperation.Arguments)
                {
                    if (arg.IsImplicit && arg.Parameter != null && arg.Parameter.Type.IsEqualTo(cancellationTokenSymbol))
                    {
                        parameterIndex = invocationOperation.TargetMethod.Parameters.IndexOf(arg.Parameter);
                        parameterName = arg.Parameter.Name;
                        return true;
                    }
                }

                return false;
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (HasExplicitCancellationTokenArgument(operation))
                return;

            if (!HasAnOverloadWithCancellationToken(operation, out var newParameterIndex, out var newParameterName))
                return;

            var availableCancellationTokens = FindCancellationTokens(operation, context.CancellationToken);
            if (availableCancellationTokens.Length > 0)
            {
                context.ReportDiagnostic(s_useAnOverloadThatHasCancellationTokenWhenACancellationTokenIsAvailableRule, CreateProperties(availableCancellationTokens, newParameterIndex, newParameterName), operation, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = operation.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(s_useAnOverloadThatHasCancellationTokenRule, CreateProperties(availableCancellationTokens, newParameterIndex, newParameterName), operation, string.Join(", ", availableCancellationTokens));
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
                if (HasAnOverloadWithCancellationToken(invocation, out _, out _))
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
                var properties = CreateProperties(availableCancellationTokens, -1, null);
                context.ReportDiagnostic(s_flowCancellationTokenInAwaitForEachRuleWhenACancellationTokenIsAvailableRule, properties, op.Collection, string.Join(", ", availableCancellationTokens));
            }
            else
            {
                var parentMethod = op.GetContainingMethod(context.CancellationToken);
                if (parentMethod is not null && parentMethod.IsOverrideOrInterfaceImplementation())
                    return;

                context.ReportDiagnostic(s_flowCancellationTokenInAwaitForEachRule, op.Collection, string.Join(", ", availableCancellationTokens));
            }
        }

        private static ImmutableDictionary<string, string?> CreateProperties(string[] cancellationTokens, int parameterIndex, string? parameterName)
        {
            return ImmutableDictionary.Create<string, string?>()
                .Add("ParameterIndex", parameterIndex.ToString(CultureInfo.InvariantCulture))
                .Add("ParameterName", parameterName)
                .Add("CancellationTokens", string.Join(",", cancellationTokens));
        }

        private List<IReadOnlyList<ISymbol>>? GetMembers(ITypeSymbol symbol, int maxDepth)
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
                    return new List<IReadOnlyList<ISymbol>>(capacity: 1) { Array.Empty<ISymbol>() };

                var result = new List<IReadOnlyList<ISymbol>>();
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
                        result.Add(new ISymbol[] { member });
                    }
                    else
                    {
                        var typeMembers = GetMembers(memberTypeSymbol, maxDepth - 1);
                        if (typeMembers != null)
                        {
                            foreach (var objectMember in typeMembers)
                            {
                                result.Add(Prepend(member, objectMember).ToList());
                            }
                        }
                    }
                }
                return result;
            });
        }

        private string[] FindCancellationTokens(IOperation operation, CancellationToken cancellationToken)
        {
            var isStatic = IsStaticMember(operation, cancellationToken);

            var availableSymbols = new List<NameAndType>();
            GetParameters(availableSymbols, operation, cancellationToken);
            GetVariables(availableSymbols, operation);
            availableSymbols.Add(new NameAndType(name: null, GetContainingType(operation, cancellationToken)));

            var paths = new List<string>();
            foreach (var availableSymbol in availableSymbols)
            {
                if (availableSymbol.TypeSymbol == null)
                    continue;

                var members = GetMembers(availableSymbol.TypeSymbol, maxDepth: 1);
                if (members != null)
                {
                    foreach (var member in members)
                    {
                        if (!AreAllSymbolsAccessibleFromOperation(member, operation))
                            continue;

                        if (availableSymbol.Name == null && isStatic && member.Count > 0 && !member[0].IsStatic)
                            continue;

                        var fullPath = ComputeFullPath(availableSymbol.Name, member);
                        paths.Add(fullPath);
                    }
                }
            }

            if (paths.Count == 0)
                return Array.Empty<string>();

            return paths.OrderBy(value => value.Count(c => c == '.')).ThenBy(value => value, StringComparer.Ordinal).ToArray();

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
                if (prefix == null)
                    return string.Join(".", symbols.Select(symbol => symbol.Name));

                var suffix = string.Join(".", symbols.Select(symbol => symbol.Name));
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
            if (ancestor == null)
                return null;

            return operation.SemanticModel!.GetDeclaredSymbol(ancestor, cancellationToken) as ITypeSymbol;
        }

        private static bool IsStaticMember(IOperation operation, CancellationToken cancellationToken)
        {
            var memberDeclarationSyntax = operation.Syntax.Ancestors().FirstOrDefault(syntax => syntax is MemberDeclarationSyntax);
            if (memberDeclarationSyntax == null)
                return false;

            var symbol = operation.SemanticModel!.GetDeclaredSymbol(memberDeclarationSyntax, cancellationToken);
            if (symbol == null)
                return false;

            return symbol.IsStatic;
        }

        private static void GetParameters(List<NameAndType> result, IOperation operation, CancellationToken cancellationToken)
        {
            var semanticModel = operation.SemanticModel!;
            var node = operation.Syntax;
            while (node != null)
            {
                switch (node)
                {
                    case AccessorDeclarationSyntax accessor:
                        {
                            if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                            {
                                var property = node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                                if (property != null)
                                {
                                    var symbol = operation.SemanticModel.GetDeclaredSymbol(property, cancellationToken);
                                    if (symbol != null)
                                    {
                                        result.Add(new NameAndType("value", symbol.Type));
                                    }
                                }
                            }

                            break;
                        }

                    case PropertyDeclarationSyntax _:
                        return;

                    case IndexerDeclarationSyntax indexerDeclarationSyntax:
                        {
                            var symbol = semanticModel.GetDeclaredSymbol(indexerDeclarationSyntax, cancellationToken);
                            if (symbol != null)
                            {
                                foreach (var parameter in symbol.Parameters)
                                    result.Add(new NameAndType(parameter.Name, parameter.Type));
                            }

                            return;
                        }

                    case MethodDeclarationSyntax methodDeclaration:
                        {
                            var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                            if (symbol != null)
                            {
                                foreach (var parameter in symbol.Parameters)
                                {
                                    result.Add(new NameAndType(parameter.Name, parameter.Type));
                                }
                            }

                            return;
                        }

                    case LocalFunctionStatementSyntax localFunctionStatement:
                        {
                            if (semanticModel.GetDeclaredSymbol(localFunctionStatement, cancellationToken) is IMethodSymbol symbol)
                            {
                                foreach (var parameter in symbol.Parameters)
                                    result.Add(new NameAndType(parameter.Name, parameter.Type));
                            }

                            break;
                        }

                    case ConstructorDeclarationSyntax constructorDeclaration:
                        {
                            var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration, cancellationToken);
                            if (symbol != null)
                            {
                                foreach (var parameter in symbol.Parameters)
                                {
                                    result.Add(new NameAndType(parameter.Name, parameter.Type));
                                }
                            }

                            return;
                        }
                }

                node = node.Parent;
            }
        }

        private static void GetVariables(List<NameAndType> result, IOperation operation)
        {
            var previousOperation = operation;
            var currentOperation = operation.Parent;

            while (currentOperation != null)
            {
                if (currentOperation.Kind == OperationKind.Block)
                {
                    var blockOperation = (IBlockOperation)currentOperation;
                    foreach (var childOperation in blockOperation.Operations)
                    {
                        if (childOperation == previousOperation)
                            break;

                        switch (childOperation)
                        {
                            case IVariableDeclarationGroupOperation variableDeclarationGroupOperation:
                                foreach (var declaration in variableDeclarationGroupOperation.Declarations)
                                {
                                    foreach (var variable in declaration.GetDeclaredVariables())
                                    {
                                        result.Add(new NameAndType(variable.Name, variable.Type));
                                    }
                                }
                                break;

                            case IVariableDeclarationOperation variableDeclarationOperation:
                                foreach (var variable in variableDeclarationOperation.GetDeclaredVariables())
                                {
                                    result.Add(new NameAndType(variable.Name, variable.Type));
                                }
                                break;
                        }
                    }
                }

                previousOperation = currentOperation;
                currentOperation = currentOperation.Parent;
            }
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
