using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseInKeywordForInParameterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseInKeywordForInParameter, RuleIdentifiers.UseInKeywordToSelectInOverload);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (root is null || diagnostic is null)
            return;

        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<ArgumentSyntax>() is not { } argument)
            return;

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(argument, context.CancellationToken) is not IArgumentOperation operation)
            return;

        if (!IsVariableReference(operation.Value))
            return;

        var title = "Add in keyword";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => AddInKeywordAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static bool IsVariableReference(IOperation operation)
    {
        while (true)
        {
            switch (operation)
            {
                case IConversionOperation conversionOperation:
                    if (!conversionOperation.Conversion.IsIdentity)
                        return false;

                    operation = conversionOperation.Operand;
                    continue;
                case ILocalReferenceOperation:
                case IParameterReferenceOperation:
                case IFieldReferenceOperation:
                case IArrayElementReferenceOperation:
                case IInstanceReferenceOperation:
                    return true;
                default:
                    return false;
            }
        }
    }

    private static async Task<Document> AddInKeywordAsync(Document document, TextSpan argumentSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        if (root.FindNode(argumentSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<ArgumentSyntax>() is not { } argument)
            return document;

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(argument, argument.WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
        return editor.GetChangedDocument();
    }
}
