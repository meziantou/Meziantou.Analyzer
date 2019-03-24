using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

            var cancellationTokens = string.Join(",", FindCancellationTokens(operation, analyzerContext));
            if (string.IsNullOrEmpty(cancellationTokens))
            {
                cancellationTokens = "CancellationToken.None";
            }

            context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), cancellationTokens));
        }

        private static IEnumerable<string> FindCancellationTokens(IInvocationOperation operation, AnalyzerContext context)
        {
            // Should explore the properties of the objects
            // Should be accessible (operation.SemanticModel.IsAccessible)

            // Property of type CancellationToken (static or instance if method is not static)
            // Variable of type CancellationToken
            // Parameter of type CancellationToken
            // TODO test with CancellationTokenSource, HttpContext

            var parameters = GetParameters(operation);
            foreach (var (parameterName, parameterType) in parameters)
            {
                foreach (var member in context.GetMembers(parameterType))
                {
                    if (member.All(IsAccessible))
                    {
                        yield return ComputeFullPath(parameterName, member);
                    }
                }
            }

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

        //private static IEnumerable<ISymbol> GetMembers(IOperation operation)
        //{
        //    var ancestor = operation.Syntax.Ancestors().FirstOrDefault(node => node is ClassDeclarationSyntax || node is StructDeclarationSyntax);
        //    if (ancestor == null)
        //        yield break;

        //    return operation.SemanticModel.GetDeclaredSymbol(ancestor) as INamedTypeSymbol;
        //}

        private static IEnumerable<(string name, ITypeSymbol type)> GetParameters(IOperation operation)
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
                                yield return ("value", symbol.Type);
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
                        yield return (parameter.Name, parameter.Type);

                    yield break;
                }
                else if (node is MethodDeclarationSyntax methodDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
                    foreach (var parameter in symbol.Parameters)
                        yield return (parameter.Name, parameter.Type);

                    yield break;
                }
                else if (node is LocalFunctionStatementSyntax localFunctionStatement)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(localFunctionStatement) as IMethodSymbol;
                    foreach (var parameter in symbol.Parameters)
                        yield return (parameter.Name, parameter.Type);
                }
                else if (node is ConstructorDeclarationSyntax constructorDeclaration)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(constructorDeclaration);
                    foreach (var parameter in symbol.Parameters)
                        yield return (parameter.Name, parameter.Type);

                    yield break;
                }

                node = node.Parent;
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
    }
}
