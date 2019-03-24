﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseAnOverloadThatHaveCancellationTokenAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseAnOverloadThatHaveCancellationToken,
            title: "Use a cancellation token",
            messageFormat: "Specify a CancellationToken ({0})",
            RuleCategories.Usage,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseAnOverloadThatHaveCancellationToken));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                if (analyzerContext.CancellationTokenSymbol == null)
                    return;

                ctx.RegisterOperationAction(c => Analyze(c, analyzerContext), OperationKind.Invocation);
            });
        }

        private static void Analyze(OperationAnalysisContext context, AnalyzerContext analyzerContext)
        {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;

            if (operation.Arguments.Any(arg => arg.Type.IsEqualsTo(analyzerContext.CancellationTokenSymbol)))
                return;

            if (!UseStringComparisonAnalyzer.HasOverloadWithAdditionalParameterOfType(operation, analyzerContext.CancellationTokenSymbol))
                return;

            var cancellationTokens = string.Join(", ", FindCancellationTokens(operation, analyzerContext));
            if (string.IsNullOrEmpty(cancellationTokens))
            {
                cancellationTokens = "CancellationToken.None";
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), cancellationTokens));
        }

        private static IEnumerable<string> FindCancellationTokens(IInvocationOperation operation, AnalyzerContext context)
        {
            var isStatic = IsStaticMember(operation);

            var all = GetParameters(operation)
                .Concat(GetVariables(operation))
                .Concat(new[] { new NameAndType(name: null, GetContainingType(operation)) });

            return from item in all
                   let members = context.GetMembers(item.TypeSymbol)
                   from member in members
                   where member.All(IsAccessible) && (item.Name != null || !isStatic || (member.FirstOrDefault()?.IsStatic ?? true))
                   let fullPath = ComputeFullPath(item.Name, member)
                   orderby fullPath.Count(c => c == '.'), fullPath
                   select fullPath;

            string ComputeFullPath(string prefix, IEnumerable<ISymbol> symbols)
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

            bool IsAccessible(ISymbol symbol)
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

        private class AnalyzerContext
        {
            private readonly ConcurrentDictionary<ITypeSymbol, IEnumerable<IEnumerable<ISymbol>>> _membersByType = new ConcurrentDictionary<ITypeSymbol, IEnumerable<IEnumerable<ISymbol>>>();

            public AnalyzerContext(Compilation compilation)
            {
                Compilation = compilation;
                CancellationTokenSymbol = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            }

            public Compilation Compilation { get; }
            public INamedTypeSymbol CancellationTokenSymbol { get; }

            public IEnumerable<IEnumerable<ISymbol>> GetMembers(ITypeSymbol symbol)
            {
                return _membersByType.GetOrAdd(symbol, s =>
                {
                    if (s.IsEqualsTo(CancellationTokenSymbol))
                    {
                        return new[] { Enumerable.Empty<ISymbol>() };
                    }

                    var result = new List<IEnumerable<ISymbol>>();

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

                        if (memberTypeSymbol.IsEqualsTo(CancellationTokenSymbol))
                        {
                            result.Add(new ISymbol[] { member });
                        }
                        else
                        {
                            foreach (var objectMembers in GetMembers(memberTypeSymbol))
                            {
                                result.Add(Prepend(member, objectMembers));
                            }
                        }
                    }

                    return result;
                });
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
