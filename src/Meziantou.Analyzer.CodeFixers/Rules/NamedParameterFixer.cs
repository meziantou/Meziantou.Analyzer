using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class NamedParameterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseNamedParameter);

    public override FixAllProvider GetFixAllProvider() => NamedParameterFixAllProvider.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (root is null || diagnostic is null)
            return;

        var argumentSpan = diagnostic.Location.SourceSpan;
        var argument = FindArgument(root, argumentSpan);
        if (argument is null || argument.NameColon is not null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (FindParameters(semanticModel, argument, context.CancellationToken) is not { } parameters)
            return;

        var index = NamedParameterAnalyzerCommon.ArgumentIndex(argument);
        if (index < 0 || index >= parameters.Length)
            return;

        var title = "Add parameter name";
        var codeAction = CodeAction.Create(
            title,
            ct => AddParameterName(context.Document, argumentSpan, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    internal static async Task<Document> AddParameterName(Document document, TextSpan argumentSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        var argument = FindArgument(root, argumentSpan);
        if (argument is null || argument.NameColon is not null)
            return document;

        if (FindParameters(semanticModel, argument, cancellationToken) is not { } parameters)
            return document;

        var index = NamedParameterAnalyzerCommon.ArgumentIndex(argument);
        if (index < 0 || index >= parameters.Length)
            return document;

        var parameter = parameters[index];
        var argumentName = parameter.Name;

        editor.ReplaceNode(argument, argument.WithNameColon(SyntaxFactory.NameColon(argumentName)));
        return editor.GetChangedDocument();
    }

    private static ArgumentSyntax? FindArgument(SyntaxNode root, TextSpan argumentSpan)
    {
        // In case the literal is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root.FindNode(argumentSpan, getInnermostNodeForTie: true);
        return nodeToFix.FirstAncestorOrSelf<ArgumentSyntax>();
    }

    private static ImmutableArray<IParameterSymbol>? FindParameters(SemanticModel semanticModel, SyntaxNode? node, CancellationToken cancellationToken)
    {
        while (node is not null)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocationExpression:
                    var method = (IMethodSymbol?)semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol;
                    return method?.Parameters;

                case ObjectCreationExpressionSyntax objectCreationExpression:
                    var ctor = (IMethodSymbol?)semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken).Symbol;
                    return ctor?.Parameters;

                case ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpression:
                    var implicitCtor = (IMethodSymbol?)semanticModel.GetSymbolInfo(implicitObjectCreationExpression, cancellationToken).Symbol;
                    return implicitCtor?.Parameters;

                case ConstructorInitializerSyntax constructorInitializerSyntax:
                    var ctor2 = (IMethodSymbol?)semanticModel.GetSymbolInfo(constructorInitializerSyntax, cancellationToken).Symbol;
                    return ctor2?.Parameters;
            }

            node = node.Parent;
        }

        return null;
    }
}
