using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var lockType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Threading.Lock");
        if (lockType is null)
            return;

        var variableDeclarator = nodeToFix.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        if (variableDeclarator is null)
            return;

        if (semanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not { } symbol)
            return;

        if (variableDeclarator.Parent is not VariableDeclarationSyntax { } declaration || declaration.Variables.Count != 1)
            return;

        const string Title = "Use System.Threading.Lock";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseLockType(context.Document, declaration, symbol, lockType, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Solution> UseLockType(Document document, VariableDeclarationSyntax declaration, ISymbol symbol, INamedTypeSymbol lockType, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var editor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(
            declaration.Type,
            ((TypeSyntax)editor.Generator.TypeExpression(lockType)).WithTriviaFrom(declaration.Type).WithAdditionalAnnotations(Formatter.Annotation));

        var variableDeclarator = declaration.Variables[0];
        if (variableDeclarator.Initializer is not null && IsObjectCreation(editor.SemanticModel, variableDeclarator.Initializer.Value, cancellationToken))
        {
            editor.ReplaceNode(variableDeclarator.Initializer.Value, ImplicitObjectCreationExpression().WithTriviaFrom(variableDeclarator.Initializer.Value));
        }

        // The field can be assigned in another document, such as another part of a partial class or a derived class
        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        foreach (var location in references.SelectMany(reference => reference.Locations))
        {
            // Source generated documents cannot be edited
            if (solution.GetDocument(location.Document.Id) is null)
                continue;

            var referenceEditor = await solutionEditor.GetDocumentEditorAsync(location.Document.Id, cancellationToken).ConfigureAwait(false);
            var reference = referenceEditor.OriginalRoot.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
            if (reference.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == reference)
            {
                reference = memberAccess;
            }

            if (reference.Parent is not AssignmentExpressionSyntax assignment || assignment.Left != reference)
                continue;

            if (!IsObjectCreation(referenceEditor.SemanticModel, assignment.Right, cancellationToken))
                continue;

            referenceEditor.ReplaceNode(assignment.Right, ImplicitObjectCreationExpression().WithTriviaFrom(assignment.Right));
        }

        return solutionEditor.GetChangedSolution();

        static bool IsObjectCreation(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
            => semanticModel.GetOperation(expression, cancellationToken) is IObjectCreationOperation { Type.SpecialType: SpecialType.System_Object };
    }
}
