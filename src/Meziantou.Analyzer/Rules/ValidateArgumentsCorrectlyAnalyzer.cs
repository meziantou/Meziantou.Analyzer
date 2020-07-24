using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ValidateArgumentsCorrectlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.ValidateArgumentsCorrectly,
            title: "Validate arguments correctly",
            messageFormat: "Validate arguments correctly",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ValidateArgumentsCorrectly));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var compilation = ctx.Compilation;
                var analyzerContext = new AnalyzerContext(compilation);

                ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            });
        }

        private sealed class AnalyzerContext
        {
            private readonly List<ISymbol> _symbols;
            private readonly INamedTypeSymbol? _argumentExceptionSymbol;

            public AnalyzerContext(Compilation compilation)
            {
                var symbols = new List<ISymbol>();
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerable"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.IEnumerator"));
                symbols.AddIfNotNull(compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1"));
                _symbols = symbols;

                _argumentExceptionSymbol = compilation.GetTypeByMetadataName("System.ArgumentException");
            }

            public bool CanContainsYield(IMethodSymbol methodSymbol)
            {
                if (!_symbols.Contains(methodSymbol.ReturnType.OriginalDefinition))
                    return false;

                return methodSymbol.Parameters.All(p => p.RefKind == RefKind.None);
            }

            internal void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
            {
                var node = (MethodDeclarationSyntax)context.Node;
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) as IMethodSymbol;
                if (methodSymbol == null || !CanContainsYield(methodSymbol))
                    return;

                var descendants = node.DescendantNodes(childNode => node == childNode || FilterDescendants(childNode)).ToList();

                var firstYieldIndex = descendants
                        .Where(node => node.IsKind(SyntaxKind.YieldReturnStatement) || node.IsKind(SyntaxKind.YieldBreakStatement))
                        .DefaultIfEmpty()
                        .Min(node => node?.SpanStart);

                if (!firstYieldIndex.HasValue)
                    return;

                var lastThrowIndex = descendants
                        .Where(node => (node.IsKind(SyntaxKind.ThrowStatement) || node.IsKind(SyntaxKind.ThrowExpression)) && IsArgumentException(context, node))
                        .DefaultIfEmpty()
                        .Max(node => GetEndOfBlockIndex(context, node));

                if (lastThrowIndex != null && firstYieldIndex != null && lastThrowIndex < firstYieldIndex)
                {
                    var properties = ImmutableDictionary.Create<string, string>()
                        .Add("Index", lastThrowIndex.Value.ToString(CultureInfo.InvariantCulture));

                    context.ReportDiagnostic(s_rule, properties, methodSymbol);
                }
            }

            public bool IsArgumentException(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
            {
                var exceptionExpression = syntaxNode switch
                {
                    ThrowStatementSyntax throwStatement => throwStatement.Expression,
                    ThrowExpressionSyntax throwExpression => throwExpression.Expression,
                    _ => null,
                };

                if (exceptionExpression == null)
                    return false;

                var type = context.SemanticModel.GetTypeInfo(exceptionExpression, context.CancellationToken).Type;
                return type != null && type.IsOrInheritFrom(_argumentExceptionSymbol);
            }

            private static bool FilterDescendants(SyntaxNode node)
            {
                return !node.IsKind(SyntaxKind.MethodDeclaration)
                    && !node.IsKind(SyntaxKind.LocalFunctionStatement);
            }

            private static int? GetEndOfBlockIndex(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
            {
                if (syntaxNode == null)
                    return null;

                var operation = context.SemanticModel.GetOperation(syntaxNode, context.CancellationToken);
                if (operation == null)
                    return null;

                while (operation != null)
                {
                    if (operation is IMethodBodyOperation)
                        break;

                    if (operation.Parent != null && operation.Parent is IBlockOperation)
                    {
                        if (operation.Parent.Parent is IMethodBodyOperation)
                            break;
                    }

                    operation = operation.Parent;
                }

                return operation?.Syntax.Span.End;
            }
        }
    }
}
