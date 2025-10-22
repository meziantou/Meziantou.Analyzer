using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringCreateInsteadOfFormattableStringFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringCreateInsteadOfFormattableString);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not InvocationExpressionSyntax nodeToFix)
            return;

        var title = "Use string.Create";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);

    }

    private static async Task<Document> Fix(Document document, InvocationExpressionSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var cultureInfo = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
        if (cultureInfo is null)
            return document;

        if (editor.SemanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation op)
            return document;

        var methodName = op.TargetMethod.Name;

        var newInvocation = generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(editor.SemanticModel.Compilation.GetSpecialType(SpecialType.System_String)), "Create"),
            generator.MemberAccessExpression(generator.TypeExpression(cultureInfo).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation), methodName == "Invariant" ? "InvariantCulture" : "CurrentCulture"),
            nodeToFix.ArgumentList.Arguments[0].Expression);

        editor.ReplaceNode(nodeToFix, newInvocation);
        return editor.GetChangedDocument();
    }
}
