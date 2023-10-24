﻿using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MakeClassStaticFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MakeClassStatic);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not ClassDeclarationSyntax nodeToFix)
            return;

        var title = "Add static modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => AddStaticModifier(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddStaticModifier(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var classNode = (ClassDeclarationSyntax)nodeToFix;

        var modifiers = classNode.Modifiers.Remove(SyntaxKind.SealedKeyword).Add(SyntaxKind.StaticKeyword);

        editor.ReplaceNode(classNode, classNode.WithModifiers(modifiers).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
