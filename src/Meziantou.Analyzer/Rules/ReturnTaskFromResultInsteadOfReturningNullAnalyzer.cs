using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReturnTaskFromResultInsteadOfReturningNullAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull,
        title: "Return Task.FromResult instead of returning null",
        messageFormat: "Return Task.FromResult instead of returning null",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (analyzerContext.TaskOfTSymbol != null || analyzerContext.TaskSymbol != null)
            {
                context.RegisterOperationAction(analyzerContext.AnalyzeReturnOperation, OperationKind.Return);
            }
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
        }

        public INamedTypeSymbol? TaskSymbol { get; }
        public INamedTypeSymbol? TaskOfTSymbol { get; }

        public void AnalyzeReturnOperation(OperationAnalysisContext context)
        {
            var operation = (IReturnOperation)context.Operation;
            if (!IsTaskType(operation.ReturnedValue?.Type))
                return;

            if (!MayBeNullValue(operation))
                return;

            // Find the owning symbol and check if it returns a task and doesn't use the async keyword
            var methodSymbol = FindContainingMethod(operation, context.CancellationToken);
            if (methodSymbol == null || !IsTaskType(methodSymbol.ReturnType))
                return;

            context.ReportDiagnostic(s_rule, operation);
        }

        private bool MayBeNullValue([NotNullWhen(true)] IOperation? operation)
        {
            if (operation == null)
                return false;

            if (operation is IReturnOperation returnOperation)
            {
                operation = returnOperation.ReturnedValue;
                if (operation == null)
                    return false;
            }

            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value == null)
                return true;

            if (operation is IConversionOperation conversion)
            {
                if (!IsTaskType(conversion.Type))
                    return false;

                return MayBeNullValue(conversion.Operand);
            }

            if (operation is IConditionalAccessOperation conditionalAccess)
            {
                return MayBeNullValue(conditionalAccess.Operation) || MayBeNullValue(conditionalAccess.WhenNotNull);
            }
            else if (operation is IConditionalOperation conditional)
            {
                return MayBeNullValue(conditional.WhenTrue) || MayBeNullValue(conditional.WhenFalse);
            }
            else if (operation is ISwitchExpressionOperation switchExpression)
            {
                foreach (var arm in switchExpression.Arms)
                {
                    if (MayBeNullValue(arm.Value))
                        return true;
                }
            }

            return false;
        }

        private bool IsTaskType(ITypeSymbol? symbol)
        {
            if (symbol == null)
                return false;

            return symbol.IsEqualTo(TaskSymbol) || symbol.OriginalDefinition.IsEqualTo(TaskOfTSymbol);
        }
    }

    internal static IMethodSymbol? FindContainingMethod(IOperation operation, CancellationToken cancellationToken)
    {
        return FindContainingMethod(operation.SemanticModel, operation.Syntax, cancellationToken);
    }

    internal static IMethodSymbol? FindContainingMethod(SemanticModel? semanticModel, SyntaxNode? syntaxNode, CancellationToken cancellationToken)
    {
        if (semanticModel == null)
            return null;

        while (syntaxNode != null)
        {
            if (syntaxNode.IsKind(SyntaxKind.AnonymousMethodExpression))
            {
                var node = (AnonymousMethodExpressionSyntax)syntaxNode;
                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || syntaxNode.IsKind(SyntaxKind.SimpleLambdaExpression))
            {
                var node = (LambdaExpressionSyntax)syntaxNode;
                if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.LocalFunctionStatement))
            {
                var node = (LocalFunctionStatementSyntax)syntaxNode;
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }
            else if (syntaxNode.IsKind(SyntaxKind.MethodDeclaration))
            {
                var node = (MethodDeclarationSyntax)syntaxNode;
                if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is IMethodSymbol methodSymbol)
                    return methodSymbol;

                return null;
            }

            syntaxNode = syntaxNode.Parent;
        }

        return null;
    }
}
