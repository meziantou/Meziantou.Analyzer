using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseRegexExplicitCaptureOptionsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseRegexExplicitCaptureOptions);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (!TryGetOptionsExpression(nodeToFix, semanticModel, context.CancellationToken, out var expressionToFix))
            return;

        var title = "Add RegexOptions.ExplicitCapture";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => AddExplicitCaptureOption(context.Document, expressionToFix, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> AddExplicitCaptureOption(Document document, ExpressionSyntax expressionToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var regexOptionsType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions");
        if (regexOptionsType is null)
            return document;

        var explicitCapture = (ExpressionSyntax)editor.Generator.MemberAccessExpression(
            editor.Generator.TypeExpression(regexOptionsType, addImport: true).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation),
            "ExplicitCapture");
        var updatedExpression = SyntaxFactory.BinaryExpression(
            SyntaxKind.BitwiseOrExpression,
            expressionToFix,
            explicitCapture).WithTriviaFrom(expressionToFix);

        editor.ReplaceNode(expressionToFix, updatedExpression);
        return editor.GetChangedDocument();
    }

    private static bool TryGetOptionsExpression(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax expression)
    {
        if (node.FirstAncestorOrSelf<AttributeSyntax>() is AttributeSyntax attribute && attribute.ArgumentList is not null)
        {
            var regexOptionsType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions");
            if (regexOptionsType is not null)
            {
                if (semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol is IMethodSymbol constructor)
                {
                    foreach (var argument in attribute.ArgumentList.Arguments)
                    {
                        IParameterSymbol? parameter = null;
                        if (argument.NameEquals is not null)
                        {
                            parameter = constructor.Parameters.FirstOrDefault(p => p.Name == argument.NameEquals.Name.Identifier.ValueText);
                        }
                        else
                        {
                            var argumentIndex = attribute.ArgumentList.Arguments.IndexOf(argument);
                            if (argumentIndex >= 0 && argumentIndex < constructor.Parameters.Length)
                            {
                                parameter = constructor.Parameters[argumentIndex];
                            }
                        }

                        if (parameter?.Type.IsEqualTo(regexOptionsType) is true)
                        {
                            expression = argument.Expression;
                            return true;
                        }
                    }
                }
            }
        }

        var argumentSyntax = node.FirstAncestorOrSelf<ArgumentSyntax>();
        if (argumentSyntax is not null && semanticModel.GetOperation(argumentSyntax, cancellationToken) is IArgumentOperation argumentOp)
        {
            var regexOptionsType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions");
            if (argumentOp.Parameter?.Type.IsEqualTo(regexOptionsType) is true)
            {
                expression = argumentSyntax.Expression;
                return true;
            }
        }

        expression = null!;
        return false;
    }
}
