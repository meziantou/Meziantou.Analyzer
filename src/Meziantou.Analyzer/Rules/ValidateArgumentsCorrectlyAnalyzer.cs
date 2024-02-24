using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidateArgumentsCorrectlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ValidateArgumentsCorrectly,
        title: "Validate arguments correctly in iterator methods",
        messageFormat: "Validate arguments correctly in iterator methods",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ValidateArgumentsCorrectly));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
        private readonly HashSet<ISymbol> _symbols;
        private readonly INamedTypeSymbol? _argumentExceptionSymbol;

        public AnalyzerContext(Compilation compilation)
        {
            var symbols = new List<ISymbol>();
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.IEnumerable"));
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerable`1"));
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"));
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.IEnumerator"));
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEnumerator`1"));
            symbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1"));
            _symbols = new HashSet<ISymbol>(symbols, SymbolEqualityComparer.Default);

            _argumentExceptionSymbol = compilation.GetBestTypeByMetadataName("System.ArgumentException");
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
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (methodSymbol is null || !CanContainsYield(methodSymbol))
                return;

            var descendants = node.DescendantNodes(childNode => node == childNode || FilterDescendants(childNode)).ToList();

            var firstYieldIndex = descendants
                    .Where(node => node.IsKind(SyntaxKind.YieldReturnStatement) || node.IsKind(SyntaxKind.YieldBreakStatement))
                    .DefaultIfEmpty()
                    .Min(node => node?.SpanStart);

            if (!firstYieldIndex.HasValue)
                return;

            var lastThrowIndex = descendants
                    .Where(node => IsArgumentValidation(context, node))
                    .DefaultIfEmpty()
                    .Max(node => GetEndOfBlockIndex(context, node));

            if (lastThrowIndex is not null && firstYieldIndex is not null && lastThrowIndex < firstYieldIndex)
            {
                var properties = ImmutableDictionary.Create<string, string?>()
                    .Add("Index", lastThrowIndex.Value.ToString(CultureInfo.InvariantCulture));

                context.ReportDiagnostic(Rule, properties, methodSymbol);
            }
        }

        private bool IsArgumentValidation(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            if ((node.IsKind(SyntaxKind.ThrowStatement) || node.IsKind(SyntaxKind.ThrowExpression)) && IsArgumentException(context, node))
                return true;
            
            if (node is InvocationExpressionSyntax invocationExpression)
            {
                if (context.SemanticModel.GetOperation(invocationExpression, context.CancellationToken) is IInvocationOperation operation)
                {
                    var targetMethod = operation.TargetMethod;
                    return targetMethod.IsStatic &&
                        targetMethod.ContainingType.IsOrInheritFrom(_argumentExceptionSymbol) &&
                        targetMethod.Name.Contains("Throw", System.StringComparison.Ordinal);
                }
            }

            return false;
        }

        public bool IsArgumentException(SyntaxNodeAnalysisContext context, SyntaxNode syntaxNode)
        {
            var exceptionExpression = syntaxNode switch
            {
                ThrowStatementSyntax throwStatement => throwStatement.Expression,
                ThrowExpressionSyntax throwExpression => throwExpression.Expression,
                _ => null,
            };

            if (exceptionExpression is null)
                return false;

            var type = context.SemanticModel.GetTypeInfo(exceptionExpression, context.CancellationToken).Type;
            return type is not null && type.IsOrInheritFrom(_argumentExceptionSymbol);
        }

        private static bool FilterDescendants(SyntaxNode node)
        {
            return !node.IsKind(SyntaxKind.MethodDeclaration)
                && !node.IsKind(SyntaxKind.LocalFunctionStatement);
        }

        private static int? GetEndOfBlockIndex(SyntaxNodeAnalysisContext context, SyntaxNode? syntaxNode)
        {
            if (syntaxNode is null)
                return null;

            var operation = context.SemanticModel.GetOperation(syntaxNode, context.CancellationToken);
            if (operation is null)
                return null;

            while (operation is not null)
            {
                if (operation is IMethodBodyOperation)
                    break;

                if (operation.Parent is not null && operation.Parent is IBlockOperation)
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
