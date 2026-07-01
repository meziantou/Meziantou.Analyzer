#if CSHARP12_OR_GREATER
using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RedundantBackingFieldForPrimaryConstructorParameterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RedundantBackingFieldForPrimaryConstructorParameter);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (node is null)
            return;

        var declarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        var fieldDecl = node.FirstAncestorOrSelf<FieldDeclarationSyntax>();
        if (fieldDecl is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        IFieldSymbol? fieldSymbol = null;
        if (declarator is not null)
        {
            fieldSymbol = semanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) as IFieldSymbol;
        }

        fieldSymbol ??= semanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0], context.CancellationToken) as IFieldSymbol;
        if (fieldSymbol is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove redundant field",
                ct => RemoveFieldAsync(context.Document, fieldDecl, declarator, ct),
                equivalenceKey: "RemoveField"),
            context.Diagnostics);

        var (paramSymbol, parameterName) = ResolveParameter(semanticModel, fieldDecl, declarator, context.CancellationToken);
        if (paramSymbol is null || parameterName is null)
            return;

        if (!SymbolEqualityComparer.IncludeNullability.Equals(fieldSymbol.Type, paramSymbol.Type))
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use parameter directly",
                ct => RemoveFieldAndRewriteReferencesAsync(context.Document, fieldSymbol, parameterName, fieldDecl, declarator, ct),
                equivalenceKey: "UseParameterDirectly"),
            context.Diagnostics);
    }

    private static (IParameterSymbol? Symbol, string? Name) ResolveParameter(SemanticModel semanticModel, FieldDeclarationSyntax fieldDecl, VariableDeclaratorSyntax? declarator, CancellationToken cancellationToken)
    {
        var targetDeclarator = declarator ?? fieldDecl.Declaration.Variables[0];
        var initializer = targetDeclarator.Initializer?.Value;
        if (initializer is null)
            return (null, null);

        var operation = semanticModel.GetOperation(initializer, cancellationToken);
        var value = operation;
        while (value is IConversionOperation conversion
               && conversion.IsImplicit
               && conversion.Conversion.IsImplicit
               && !conversion.Conversion.IsUserDefined)
        {
            value = conversion.Operand;
        }

        if (value is not IParameterReferenceOperation parameterReference)
            return (null, null);

        if (parameterReference.Parameter.ContainingSymbol is not IMethodSymbol methodSymbol)
            return (null, null);

        if (!methodSymbol.IsPrimaryConstructor(cancellationToken, includeRecordDeclarations: true))
            return (null, null);

        return (parameterReference.Parameter, parameterReference.Parameter.Name);
    }

    private static async Task<Document> RemoveFieldAsync(Document document, FieldDeclarationSyntax fieldDecl, VariableDeclaratorSyntax? declarator, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (declarator is not null && fieldDecl.Declaration.Variables.Count > 1)
        {
            var remaining = fieldDecl.Declaration.Variables.Remove(declarator);
            editor.ReplaceNode(fieldDecl, fieldDecl.WithDeclaration(fieldDecl.Declaration.WithVariables(remaining)));
        }
        else
        {
            editor.RemoveNode(fieldDecl);
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Solution> RemoveFieldAndRewriteReferencesAsync(Document document, IFieldSymbol fieldSymbol, string parameterName, FieldDeclarationSyntax fieldDecl, VariableDeclaratorSyntax? declarator, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var references = await SymbolFinder.FindReferencesAsync(fieldSymbol, solution, cancellationToken).ConfigureAwait(false);

        var editsByDocument = new Dictionary<DocumentId, List<SyntaxNode>>();

        foreach (var refLocation in references.SelectMany(r => r.Locations))
        {
            if (refLocation.Document.Id == document.Id)
                continue;

            var refDocument = solution.GetDocument(refLocation.Document.Id);
            if (refDocument is null)
                continue;

            var refRoot = await refDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (refRoot is null)
                continue;

            var refNode = refRoot.FindNode(refLocation.Location.SourceSpan, getInnermostNodeForTie: true);
            if (refNode is null)
                continue;

            var targetNode = ResolveReferenceTargetNode(refNode);
            if (targetNode is null)
                continue;

            if (!editsByDocument.TryGetValue(refDocument.Id, out var list))
            {
                list = [];
                editsByDocument[refDocument.Id] = list;
            }

            list.Add(targetNode);
        }

        foreach (var (docId, nodes) in editsByDocument)
        {
            var targetDoc = solution.GetDocument(docId);
            if (targetDoc is null)
                continue;

            var editor = await DocumentEditor.CreateAsync(targetDoc, cancellationToken).ConfigureAwait(false);
            foreach (var node in nodes)
            {
                editor.ReplaceNode(node, SyntaxFactory.IdentifierName(parameterName));
            }

            solution = editor.GetChangedDocument().Project.Solution;
        }

        var fieldEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var fieldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (fieldRoot is not null)
        {
            foreach (var refLocation in references.SelectMany(r => r.Locations).Where(l => l.Document.Id == document.Id))
            {
                var refNode = fieldRoot.FindNode(refLocation.Location.SourceSpan, getInnermostNodeForTie: true);
                if (refNode is null)
                    continue;

                if (refNode.Ancestors().OfType<VariableDeclaratorSyntax>().Any(d => d.Initializer?.Value.Contains(refNode) == true))
                    continue;

                var targetNode = ResolveReferenceTargetNode(refNode);
                if (targetNode is not null)
                {
                    fieldEditor.ReplaceNode(targetNode, SyntaxFactory.IdentifierName(parameterName));
                }
            }
        }

        RemoveFieldDeclarator(fieldEditor, fieldDecl, declarator);

        return fieldEditor.GetChangedDocument().Project.Solution;
    }

    private static SyntaxNode? ResolveReferenceTargetNode(SyntaxNode refNode)
    {
        if (refNode.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == refNode)
        {
            // `this.x` is invalid for primary-ctor params (CS1061), so replace the whole member-access.
            if (memberAccess.Expression is ThisExpressionSyntax)
                return memberAccess;
        }

        if (refNode is IdentifierNameSyntax)
            return refNode;

        return null;
    }

    private static void RemoveFieldDeclarator(DocumentEditor editor, FieldDeclarationSyntax fieldDecl, VariableDeclaratorSyntax? declarator)
    {
        if (declarator is not null && fieldDecl.Declaration.Variables.Count > 1)
        {
            var remaining = fieldDecl.Declaration.Variables.Remove(declarator);
            editor.ReplaceNode(fieldDecl, fieldDecl.WithDeclaration(fieldDecl.Declaration.WithVariables(remaining)));
        }
        else
        {
            editor.RemoveNode(fieldDecl);
        }
    }
}
#endif
