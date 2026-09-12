namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class SimplifyCallerArgumentExpressionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SimplifyCallerArgumentExpression);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: false) is not ArgumentSyntax nodeToFix)
            return;

        if (nodeToFix.Parent is not ArgumentListSyntax argumentList)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        // Removing the argument shifts the following positional arguments, so they must be named to keep binding to the same parameters
        if (GetArgumentsToName(semanticModel, argumentList, nodeToFix, context.CancellationToken) is not { } argumentsToName)
            return;

        var title = "Remove argument";
        context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveArgument(context.Document, nodeToFix, argumentsToName, ct), equivalenceKey: title), context.Diagnostics);
    }

    private static List<(ArgumentSyntax Argument, string ParameterName)>? GetArgumentsToName(SemanticModel semanticModel, ArgumentListSyntax argumentList, ArgumentSyntax argumentToRemove, CancellationToken cancellationToken)
    {
        var arguments = argumentList.Arguments;
        var index = arguments.IndexOf(argumentToRemove);
        if (index < 0)
            return null;

        var result = new List<(ArgumentSyntax, string)>();
        Dictionary<SyntaxNode, string>? parameterNames = null;
        for (var i = index + 1; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (argument.NameColon is not null)
                continue;

            parameterNames ??= GetParameterNames(semanticModel, argumentList, cancellationToken);
            if (parameterNames is null || !parameterNames.TryGetValue(argument, out var parameterName))
                return null;

            result.Add((argument, parameterName));
        }

        return result;
    }

    private static Dictionary<SyntaxNode, string>? GetParameterNames(SemanticModel semanticModel, ArgumentListSyntax argumentList, CancellationToken cancellationToken)
    {
        if (argumentList.Parent is null)
            return null;

        if (semanticModel.GetOperation(argumentList.Parent, cancellationToken) is not IInvocationOperation invocation)
            return null;

        var result = new Dictionary<SyntaxNode, string>();
        foreach (var argument in invocation.Arguments)
        {
            // Only the arguments written by the user can be named. In particular, the arguments of a
            // params parameter in its expanded form cannot be converted to a named argument.
            if (argument.ArgumentKind is not ArgumentKind.Explicit || argument.Parameter is null)
                continue;

            result[argument.Syntax] = argument.Parameter.Name;
        }

        return result;
    }

    private static async Task<Document> RemoveArgument(Document document, ArgumentSyntax argumentToRemove, List<(ArgumentSyntax Argument, string ParameterName)> argumentsToName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var (argument, parameterName) in argumentsToName)
        {
            editor.ReplaceNode(argument, argument.WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(CreateIdentifier(parameterName)))));
        }

        editor.RemoveNode(argumentToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        return editor.GetChangedDocument();
    }

    private static SyntaxToken CreateIdentifier(string parameterName)
    {
        return SyntaxFacts.GetKeywordKind(parameterName) is SyntaxKind.None
            ? SyntaxFactory.Identifier(parameterName)
            : SyntaxFactory.VerbatimIdentifier(leading: default, text: parameterName, valueText: parameterName, trailing: default);
    }
}
