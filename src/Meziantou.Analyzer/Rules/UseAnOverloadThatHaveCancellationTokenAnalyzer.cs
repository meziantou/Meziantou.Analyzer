using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseAnOverloadThatHaveCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_useAnOverloadThatHaveCancellationTokenRule = new DiagnosticDescriptor(
            RuleIdentifiers.UseAnOverloadThatHaveCancellationToken,
            title: "Use a cancellation token",
            messageFormat: "Specify a CancellationToken",
            RuleCategories.Usage,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHaveCancellationToken));

        private static readonly DiagnosticDescriptor s_useAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailableRule = new DiagnosticDescriptor(
            RuleIdentifiers.UseAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailable,
            title: "Use a cancellation token",
            messageFormat: "Specify a CancellationToken ({0})",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailable));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_useAnOverloadThatHaveCancellationTokenRule, s_useAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailableRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                if (analyzerContext.CancellationTokenSymbol == null)
                    return;

                ctx.RegisterOperationAction(analyzerContext.Analyze, OperationKind.Invocation);
            });
        }

        private sealed class AnalyzerContext
        {
            private readonly ConcurrentDictionary<ITypeSymbol, IEnumerable<IReadOnlyList<ISymbol>>> _membersByType = new ConcurrentDictionary<ITypeSymbol, IEnumerable<IReadOnlyList<ISymbol>>>();

            public AnalyzerContext(Compilation compilation)
            {
                Compilation = compilation;
                CancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                CancellationTokenSourceSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationTokenSource");
                TaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                TaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            }

            public Compilation Compilation { get; }
            public INamedTypeSymbol CancellationTokenSymbol { get; }
            public INamedTypeSymbol CancellationTokenSourceSymbol { get; }
            private INamedTypeSymbol TaskSymbol { get; }
            private INamedTypeSymbol TaskOfTSymbol { get; }

            public void Analyze(OperationAnalysisContext context)
            {
                var operation = (IInvocationOperation)context.Operation;
                var method = operation.TargetMethod;

                if (operation.Arguments.Any(arg => !arg.IsImplicit && arg.Parameter.Type.IsEqualTo(CancellationTokenSymbol)))
                    return;

                if (string.Equals(method.Name, nameof(CancellationTokenSource.CreateLinkedTokenSource), StringComparison.Ordinal) && method.ContainingType.IsEqualTo(CancellationTokenSourceSymbol))
                    return;

                var isImplicitlyDeclared = operation.Arguments.Any(arg => arg.IsImplicit && arg.Parameter.Type.IsEqualTo(CancellationTokenSymbol));
                if (!isImplicitlyDeclared && !operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(context.Compilation, CancellationTokenSymbol))
                    return;

                var possibleCancellationTokens = string.Join(", ", FindCancellationTokens(operation));
                if (!string.IsNullOrEmpty(possibleCancellationTokens))
                {
                    context.ReportDiagnostic(s_useAnOverloadThatHaveCancellationTokenWhenACancellationTokenIsAvailableRule, operation, possibleCancellationTokens);
                }
                else
                {
                    context.ReportDiagnostic(s_useAnOverloadThatHaveCancellationTokenRule, operation, possibleCancellationTokens);
                }
            }

            private IEnumerable<IReadOnlyList<ISymbol>> GetMembers(ITypeSymbol symbol, int maxDepth)
            {
                if (maxDepth < 0 || symbol == null)
                    return Enumerable.Empty<IReadOnlyList<ISymbol>>();

                if (symbol.IsEqualTo(TaskSymbol) || symbol.OriginalDefinition.IsEqualTo(TaskOfTSymbol))
                    return Enumerable.Empty<IReadOnlyList<ISymbol>>();

                return _membersByType.GetOrAdd(symbol, s =>
                {
                    if (s.IsEqualTo(CancellationTokenSymbol))
                    {
                        return new[] { Array.Empty<ISymbol>() };
                    }

                    var result = new List<IReadOnlyList<ISymbol>>();

                    var members = s.GetMembers();
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
                            foreach (var objectMembers in GetMembers(memberTypeSymbol, maxDepth - 1))
                            {
                                result.Add(Prepend(member, objectMembers).ToList());
                            }
                        }
                    }

                    return result;
                });
            }

            private IEnumerable<string> FindCancellationTokens(IInvocationOperation operation)
            {
                var isStatic = IsStaticMember(operation);

                var all = GetParameters(operation)
                    .Concat(GetVariables(operation))
                    .Concat(new[] { new NameAndType(name: null, GetContainingType(operation)) });

                return from item in all
                       let members = GetMembers(item.TypeSymbol, maxDepth: 1)
                       from member in members
                       where member.All(IsSymbolAccessible) && (item.Name != null || !isStatic || (member.FirstOrDefault()?.IsStatic ?? true))
                       let fullPath = ComputeFullPath(item.Name, member)
                       orderby fullPath.Count(c => c == '.'), fullPath
                       select fullPath;

                static string ComputeFullPath(string prefix, IEnumerable<ISymbol> symbols)
                {
                    if (prefix == null)
                    {
                        return string.Join(".", symbols.Select(symbol => symbol.Name));
                    }
                    else
                    {
                        var suffix = string.Join(".", symbols.Select(symbol => symbol.Name));
                        if (string.IsNullOrEmpty(suffix))
                            return prefix;

                        return prefix + "." + suffix;
                    }
                }

                bool IsSymbolAccessible(ISymbol symbol)
                {
                    return operation.SemanticModel.IsAccessible(operation.Syntax.Span.Start, symbol);
                }
            }

            private static ITypeSymbol GetContainingType(IOperation operation)
            {
                var ancestor = operation.Syntax.Ancestors().FirstOrDefault(node => node is ClassDeclarationSyntax || node is StructDeclarationSyntax);
                if (ancestor == null)
                    return null;

                return operation.SemanticModel.GetDeclaredSymbol(ancestor) as ITypeSymbol;
            }

            private static bool IsStaticMember(IOperation operation)
            {
                var memberDeclarationSyntax = operation.Syntax.Ancestors().FirstOrDefault(syntax => syntax is MemberDeclarationSyntax);
                if (memberDeclarationSyntax == null)
                    return false;

                var symbol = operation.SemanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
                if (symbol == null)
                    return false;

                return symbol.IsStatic;
            }

            private static IEnumerable<NameAndType> GetParameters(IOperation operation)
            {
                var semanticModel = operation.SemanticModel;
                var node = operation.Syntax;
                while (node != null)
                {
                    if (node is AccessorDeclarationSyntax accessor)
                    {
                        if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                        {
                            var property = node.Ancestors().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                            if (property != null)
                            {
                                var symbol = operation.SemanticModel.GetDeclaredSymbol(property);
                                if (symbol != null)
                                {
                                    yield return new NameAndType("value", symbol.Type);
                                }
                            }
                        }
                    }
                    else if (node is PropertyDeclarationSyntax)
                    {
                        yield break;
                    }
                    else if (node is IndexerDeclarationSyntax indexerDeclarationSyntax)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(indexerDeclarationSyntax);
                        foreach (var parameter in symbol.Parameters)
                            yield return new NameAndType(parameter.Name, parameter.Type);

                        yield break;
                    }
                    else if (node is MethodDeclarationSyntax methodDeclaration)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
                        foreach (var parameter in symbol.Parameters)
                            yield return new NameAndType(parameter.Name, parameter.Type);

                        yield break;
                    }
                    else if (node is LocalFunctionStatementSyntax localFunctionStatement)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(localFunctionStatement) as IMethodSymbol;
                        foreach (var parameter in symbol.Parameters)
                            yield return new NameAndType(parameter.Name, parameter.Type);
                    }
                    else if (node is ConstructorDeclarationSyntax constructorDeclaration)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
                        foreach (var parameter in symbol.Parameters)
                            yield return new NameAndType(parameter.Name, parameter.Type);

                        yield break;
                    }

                    node = node.Parent;
                }
            }

            private static IEnumerable<NameAndType> GetVariables(IOperation operation)
            {
                var previousOperation = operation;
                operation = operation.Parent;

                while (operation != null)
                {
                    if (operation is IBlockOperation blockOperation)
                    {
                        foreach (var childOperation in blockOperation.Children)
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
                                            yield return new NameAndType(variable.Name, variable.Type);
                                        }
                                    }
                                    break;

                                case IVariableDeclarationOperation variableDeclarationOperation:
                                    foreach (var variable in variableDeclarationOperation.GetDeclaredVariables())
                                    {
                                        yield return new NameAndType(variable.Name, variable.Type);
                                    }
                                    break;
                            }
                        }
                    }

                    previousOperation = operation;
                    operation = operation.Parent;
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
            public NameAndType(string name, ITypeSymbol typeSymbol)
            {
                Name = name;
                TypeSymbol = typeSymbol;
            }

            public string Name { get; }
            public ITypeSymbol TypeSymbol { get; }
        }
    }
}
