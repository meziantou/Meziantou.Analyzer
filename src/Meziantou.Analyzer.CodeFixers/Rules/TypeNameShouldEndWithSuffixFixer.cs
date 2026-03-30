using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class TypeNameShouldEndWithSuffixFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.AttributeNameShouldEndWithAttribute,
        RuleIdentifiers.ExceptionNameShouldEndWithException,
        RuleIdentifiers.EventArgsNameShouldEndWithEventArgs);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic is null)
            return;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var declaration = nodeToFix?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (declaration is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
            return;

        var suffix = diagnostic.Id switch
        {
            RuleIdentifiers.AttributeNameShouldEndWithAttribute => "Attribute",
            RuleIdentifiers.ExceptionNameShouldEndWithException => "Exception",
            RuleIdentifiers.EventArgsNameShouldEndWithEventArgs => "EventArgs",
            _ => null,
        };
        if (suffix is null || typeSymbol.Name.EndsWith(suffix, StringComparison.Ordinal))
            return;

        var newName = typeSymbol.Name + suffix;
        var title = "Rename to '" + newName + "'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => Renamer.RenameSymbolAsync(context.Document.Project.Solution, typeSymbol, new SymbolRenameOptions(), newName, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }
}
