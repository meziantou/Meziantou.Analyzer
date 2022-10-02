using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class ReturnTaskDirectlyFixer : CodeFixProvider
{
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var codeAction = CodeAction.Create("Return Task directly", ct => CreateChangedDocumentAsync(context.Document, context.Span, diagnostic.AdditionalLocations, ct), diagnostic.Id);
            context.RegisterCodeFix(codeAction, diagnostic);
        }

        return Task.CompletedTask;
    }

	private static async Task<Document> CreateChangedDocumentAsync(Document document, TextSpan span, IReadOnlyList<Location> additionalLocations, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root?.FindNode(span) is not AwaitExpressionSyntax awaitExpression)
		{
			return document;
		}

		var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
		var oldMethod = GetParentMethod(awaitExpression);
		if (MethodHasBlockBody(oldMethod) && awaitExpression.Parent is not null)
		{
			ReplaceWithReturn(editor, awaitExpression);
			var additionalAwaitExpressions = additionalLocations
				.Select(location => root.FindNode(location.SourceSpan))
				.OfType<AwaitExpressionSyntax>();
			foreach (var additionalAwaitExpression in additionalAwaitExpressions)
			{
				ReplaceWithReturn(editor, additionalAwaitExpression);
			}
		} else if (MethodHasExpressionBody(oldMethod))
		{
			editor.ReplaceNode(awaitExpression, awaitExpression.Expression);
		}

		var modifiers = GetModifiers(oldMethod);
		var asyncKeyword = modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.AsyncKeyword));
		var newModifiers = modifiers.Remove(asyncKeyword);
		editor.ReplaceNode(asyncKeyword.Parent!, (x, _) => x switch
		{
			MethodDeclarationSyntax method => method.WithModifiers(newModifiers).WithTriviaFrom(method),
			LocalFunctionStatementSyntax localFunction => localFunction.WithModifiers(newModifiers).WithTriviaFrom(localFunction),
			ParenthesizedLambdaExpressionSyntax lambda => lambda.WithModifiers(newModifiers).WithTriviaFrom(lambda),
			_ => x
		});

		return editor.GetChangedDocument();
	}

	private static bool MethodHasBlockBody(SyntaxNode method)
	{
		return method switch
		{
			MethodDeclarationSyntax m => m.Body is not null,
			LocalFunctionStatementSyntax l => l.Body is not null,
			ParenthesizedLambdaExpressionSyntax p => p.Block is not null,
			_ => false
		};
	}

	private static bool MethodHasExpressionBody(SyntaxNode method)
	{
		return method switch
		{
			MethodDeclarationSyntax m => m.ExpressionBody is not null,
			LocalFunctionStatementSyntax l => l.ExpressionBody is not null,
			ParenthesizedLambdaExpressionSyntax p => p.ExpressionBody is not null,
			_ => false
		};
	}

	private static SyntaxTokenList GetModifiers(SyntaxNode method)
	{
		return method switch
		{
			MethodDeclarationSyntax m => m.Modifiers,
			LocalFunctionStatementSyntax l => l.Modifiers,
			ParenthesizedLambdaExpressionSyntax p => p.Modifiers,
			_ => default
		};
	}

	private static void ReplaceWithReturn(DocumentEditor editor, AwaitExpressionSyntax awaitExpression)
	{
		if (awaitExpression.Parent is null)
		{
			return;
		}

		var newReturnStatement = editor.Generator.ReturnStatement(StripConfigureAwait(awaitExpression.Expression))
			.NormalizeWhitespace()
			.WithTriviaFrom(awaitExpression.Parent);
		editor.ReplaceNode(awaitExpression.Parent, newReturnStatement);

		var nextSibling = awaitExpression.Parent.GetNextSibling();
		if (nextSibling.IsKind(SyntaxKind.ReturnStatement))
		{
			editor.RemoveNode(nextSibling, SyntaxRemoveOptions.KeepNoTrivia);
		}
	}

	private static SyntaxNode GetParentMethod(ExpressionSyntax expression)
	{
		var parent = expression.Parent;
		while (parent is not null)
		{
			if (parent.IsKind(SyntaxKind.MethodDeclaration)
                || parent.IsKind(SyntaxKind.LocalFunctionStatement)
                || parent.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
			{
				return parent;
			}

			parent = parent.Parent;
		}

		throw new InvalidOperationException("Expression is not in method.");
	}

	private static ExpressionSyntax StripConfigureAwait(ExpressionSyntax expressionSyntax)
	{
		if (expressionSyntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "ConfigureAwait"} memberAccess } )
		{
			return memberAccess.Expression;
		}

		return expressionSyntax;
	}

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RuleIdentifiers.ReturnTaskDirectly);
}
