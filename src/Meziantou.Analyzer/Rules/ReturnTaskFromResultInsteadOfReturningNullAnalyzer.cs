using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.SimpleLambdaExpression);
        context.RegisterSyntaxNodeAction(AnalyzeLambda, SyntaxKind.ParenthesizedLambdaExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAnonymousMethod, SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeAnonymousMethod(SyntaxNodeAnalysisContext context)
    {
        var node = (AnonymousMethodExpressionSyntax)context.Node;
        if (node.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            return;

        if (context.Compilation == null)
            return;

        if (context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol || !IsTaskType(context.Compilation, methodSymbol.ReturnType))
            return;

        AnalyzeOperation(context, context.SemanticModel.GetOperation(node.Body, context.CancellationToken));
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        var node = (LambdaExpressionSyntax)context.Node;
        if (node.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
            return;

        if (context.Compilation == null)
            return;

        if (context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol || !IsTaskType(context.Compilation, methodSymbol.ReturnType))
            return;

        if (node.Body is BlockSyntax)
        {
            AnalyzeOperation(context, context.SemanticModel.GetOperation(node.Body, context.CancellationToken));
        }
        else if (node.Body is ExpressionSyntax expression)
        {
            var operation = context.SemanticModel.GetOperation(expression, context.CancellationToken);
            if (IsNullValue(operation))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var node = (LocalFunctionStatementSyntax)context.Node;
        if (node.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        if (context.Compilation == null || context.SemanticModel == null)
            return;

        var type = node.ReturnType;
        if (type != null && IsTaskType(context.Compilation, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
        {
            if (node.Body != null)
            {
                AnalyzeOperation(context, context.SemanticModel.GetOperation(node.Body, context.CancellationToken));
            }

            if (node.ExpressionBody != null)
            {
                AnalyzeOperation(context, context.SemanticModel.GetOperation(node.ExpressionBody, context.CancellationToken));
            }
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var node = (MethodDeclarationSyntax)context.Node;
        if (node.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        var type = node.ReturnType;
        if (type != null && context.Compilation != null && IsTaskType(context.Compilation, context.SemanticModel.GetTypeInfo(type, context.CancellationToken).Type))
        {
            if (node.Body != null)
            {
                AnalyzeOperation(context, context.SemanticModel.GetOperation(node.Body, context.CancellationToken));
            }

            if (node.ExpressionBody != null)
            {
                AnalyzeOperation(context, context.SemanticModel.GetOperation(node.ExpressionBody, context.CancellationToken));
            }
        }
    }

    private static void AnalyzeOperation(SyntaxNodeAnalysisContext context, IOperation? operation)
    {
        if (operation == null)
            return;

        foreach (var op in DescendantsAndSelf(operation, o => o is not IAnonymousFunctionOperation and not ILocalFunctionOperation))
        {
            if (op is IReturnOperation returnOperation && IsNullValue(returnOperation.ReturnedValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, op.Syntax.GetLocation()));
            }
        }
    }

    private static bool IsNullValue([NotNullWhen(true)] IOperation? operation)
    {
        if (operation == null)
            return false;

        if (operation.ConstantValue.HasValue && operation.ConstantValue.Value == null)
            return true;

        if (operation is IConditionalAccessOperation conditionalAccess)
        {
            return IsNullValue(conditionalAccess.Operation) || IsNullValue(conditionalAccess.WhenNotNull);
        }

        return false;
    }

    private static bool IsTaskType(Compilation compilation, ITypeSymbol? typeSyntax)
    {
        if (compilation is null)
            throw new ArgumentNullException(nameof(compilation));

        if (typeSyntax == null)
            return false;

        return typeSyntax.IsEqualTo(compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
               (typeSyntax.OriginalDefinition != null && typeSyntax.OriginalDefinition.IsEqualTo(compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")));
    }

    private static IEnumerable<IOperation> DescendantsAndSelf(IOperation operation, Func<IOperation, bool> processChildren)
    {
        var stack = new Stack<IOperation>();
        stack.Push(operation);
        while (stack.Any())
        {
            var op = stack.Pop();
            yield return op;
            if (processChildren(op))
            {
                foreach (var child in op.Children)
                {
                    stack.Push(child);
                }
            }
        }
    }
}
