using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix,
            RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        IMethodSymbol? methodSymbol = null;

        // Try to get the method symbol: either from a method declaration or a local function
        var declarationNode = nodeToFix.AncestorsAndSelf().FirstOrDefault(n => n is MethodDeclarationSyntax or LocalFunctionStatementSyntax);
        if (declarationNode is MethodDeclarationSyntax methodDeclaration)
        {
            methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken) as IMethodSymbol;
        }
        else if (declarationNode is LocalFunctionStatementSyntax localFunctionStatement)
        {
            methodSymbol = semanticModel.GetDeclaredSymbol(localFunctionStatement, context.CancellationToken) as IMethodSymbol;
        }

        if (methodSymbol is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            string newName;
            string title;
            if (diagnostic.Id is RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix or RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix)
            {
                newName = methodSymbol.Name + "Async";
                title = $"Rename to '{newName}'";
            }
            else
            {
                if (!methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal))
                    continue;

                newName = methodSymbol.Name[..^"Async".Length];
                title = $"Rename to '{newName}'";
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => RenameMethodAsync(context.Document, methodSymbol, newName, ct),
                    equivalenceKey: title),
                diagnostic);
        }
    }

    private static async Task<Solution> RenameMethodAsync(Document document, IMethodSymbol methodSymbol, string newName, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        return await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);
    }
}
