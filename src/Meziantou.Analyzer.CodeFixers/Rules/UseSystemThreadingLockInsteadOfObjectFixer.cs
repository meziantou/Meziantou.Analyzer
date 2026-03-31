using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseSystemThreadingLockInsteadOfObjectFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseSystemThreadingLockInsteadOfObject);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        const string title = "Use System.Threading.Lock";
        context.RegisterCodeFix(
            CodeAction.Create(title, ct => UseLockType(context.Document, nodeToFix, ct), equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> UseLockType(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var lockType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Lock");
        if (lockType is null)
            return document;

        var variableDeclarator = nodeToFix.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        if (variableDeclarator is null)
            return document;

        if (editor.SemanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken) is not ISymbol symbol)
            return document;

        if (variableDeclarator.Parent is not VariableDeclarationSyntax declaration || declaration.Variables.Count != 1)
            return document;

        editor.ReplaceNode(
            declaration.Type,
            ((TypeSyntax)editor.Generator.TypeExpression(lockType)).WithTriviaFrom(declaration.Type).WithAdditionalAnnotations(Formatter.Annotation));

        if (variableDeclarator.Initializer is not null && IsObjectCreation(editor.SemanticModel, variableDeclarator.Initializer.Value, cancellationToken))
        {
            editor.ReplaceNode(variableDeclarator.Initializer.Value, ImplicitObjectCreationExpression().WithTriviaFrom(variableDeclarator.Initializer.Value));
        }

        foreach (var assignment in editor.OriginalRoot.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            var leftSymbol = editor.SemanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
            if (!SymbolEqualityComparer.Default.Equals(leftSymbol, symbol))
                continue;

            if (!IsObjectCreation(editor.SemanticModel, assignment.Right, cancellationToken))
                continue;

            editor.ReplaceNode(assignment.Right, ImplicitObjectCreationExpression().WithTriviaFrom(assignment.Right));
        }

        return editor.GetChangedDocument();

        static bool IsObjectCreation(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
            => semanticModel.GetOperation(expression, cancellationToken) is IObjectCreationOperation { Type.SpecialType: SpecialType.System_Object };

    }
}
