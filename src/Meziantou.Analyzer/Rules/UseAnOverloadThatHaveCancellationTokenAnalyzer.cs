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
            messageFormat: "{0}",
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

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var method = operation.TargetMethod;

            var cancellationTokenSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            if (cancellationTokenSymbol == null)
                return;

            if (operation.Arguments.Any(arg => arg.Type.IsEqualsTo(cancellationTokenSymbol)))
                return;

            if (!UseStringComparisonAnalyzer.HasOverloadWithAdditionalParameterOfType(operation, cancellationTokenSymbol))
                return;

            // TODO Check if there is a cancellation token in the context to improve the message (variable, parameter, property, HttpContext, HttpRequest.RequestAborted)
            var cancellationTokens = FindCancellationTokens(operation).ToList();

            context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
        }

        private static IEnumerable<string> FindCancellationTokens(IInvocationOperation operation)
        {
            // Should explore the properties of the objects
            // Should be accessible (operation.SemanticModel.IsAccessible)

            // Property of type CancellationToken (static or instance if method is not static)
            // Variable of type CancellationToken
            // Parameter of type CancellationToken
            // TODO test with CancellationTokenSource, HttpContext

            var parameters = GetParameters(operation);
            var members = GetMembers(operation);
            foreach (var member in members)
            {
                if (!operation.SemanticModel.IsAccessible(operation.Syntax.Span.Start, member))
                    continue;
            }


            yield break;
        }

        private static IEnumerable<ISymbol> GetMembers(IOperation operation)
        {
            var ancestor = operation.Syntax.Ancestors().FirstOrDefault(node => node is ClassDeclarationSyntax || node is StructDeclarationSyntax);
            if (ancestor == null)
                yield break;

            var symbol = operation.SemanticModel.GetDeclaredSymbol(ancestor) as INamedTypeSymbol;
            if (symbol == null)
                yield break;

            var members = symbol.GetMembers();
            foreach (var member in members)
            {
                if (member.IsImplicitlyDeclared)
                    continue;

                switch (member)
                {
                    case IPropertySymbol property:
                        if (property.GetMethod != null)
                        {
                            yield return member;
                        }

                        break;

                    case IFieldSymbol field:
                        yield return field;
                        break;
                }
            }
        }

        private static IEnumerable<(string memberPrefix, ISymbol symbol)> GetMembers(string memberPrefix, INamedTypeSymbol symbol)
        {
            if (symbol == null)
                yield break;

            var members = symbol.GetMembers();
            foreach (var member in members)
            {
                if (member.IsImplicitlyDeclared)
                    continue;

                switch (member)
                {
                    case IPropertySymbol property:
                        if (property.GetMethod != null)
                        {
                            yield return (memberPrefix, member);
                        }

                        break;

                    case IFieldSymbol field:
                        yield return (memberPrefix, field);
                        break;
                }
            }
        }

        private static IEnumerable<(string name, ISymbol type)> GetParameters(IOperation operation)
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
    }
}
