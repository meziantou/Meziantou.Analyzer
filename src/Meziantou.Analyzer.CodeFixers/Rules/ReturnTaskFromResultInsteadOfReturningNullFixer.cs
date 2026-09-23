namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ReturnTaskFromResultInsteadOfReturningNullFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReturnTaskFromResultInsteadOfReturningNull);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var returnedExpression = nodeToFix switch
        {
            ReturnStatementSyntax returnStatement => returnStatement.Expression,
            ExpressionSyntax expression => expression,
            _ => null,
        };

        if (returnedExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (ReturnTaskFromResultInsteadOfReturningNullAnalyzerCommon.FindContainingMethod(semanticModel, nodeToFix, context.CancellationToken)?.ReturnType is not INamedTypeSymbol type)
            return;

        var taskTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (taskTypeSymbol is null)
            return;

        // Only the null values are replaced, so the other branches of a conditional or switch expression are kept
        var nullExpressions = new List<SyntaxNode>();
        if (!TryCollectNullExpressions(semanticModel.GetOperation(returnedExpression, context.CancellationToken), nullExpressions) || nullExpressions.Count == 0)
            return;

        if (!type.IsGenericType)
        {
            var title = "Use Task.CompletedTask";
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseTaskCompleted(context.Document, nullExpressions, taskTypeSymbol, ct), equivalenceKey: title), context.Diagnostics);
        }
        else
        {
            var title = "Use Task.FromResult";
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseTaskFromResult(context.Document, nullExpressions, taskTypeSymbol, type, ct), equivalenceKey: title), context.Diagnostics);
        }
    }

    private static bool TryCollectNullExpressions(IOperation? operation, List<SyntaxNode> nullExpressions)
    {
        switch (operation)
        {
            case null:
                return false;

            case { ConstantValue: { HasValue: true, Value: null } }:
                nullExpressions.Add(operation.Syntax);
                return true;

            case IConversionOperation conversion:
                return TryCollectNullExpressions(conversion.Operand, nullExpressions);

            case IConditionalOperation conditional:
                return TryCollectNullExpressions(conditional.WhenTrue, nullExpressions) && TryCollectNullExpressions(conditional.WhenFalse, nullExpressions);

            case ISwitchExpressionOperation switchExpression:
                foreach (var arm in switchExpression.Arms)
                {
                    if (!TryCollectNullExpressions(arm.Value, nullExpressions))
                        return false;
                }

                return true;

            // The null value of a conditional access has no syntax node that can be replaced
            case IConditionalAccessOperation:
                return false;

            default:
                return true;
        }
    }

    private static async Task<Document> UseTaskCompleted(Document document, List<SyntaxNode> nullExpressions, INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        foreach (var nullExpression in nullExpressions)
        {
            var newExpression = generator.MemberAccessExpression(generator.TypeExpression(typeSymbol), nameof(Task.CompletedTask));
            editor.ReplaceNode(nullExpression, newExpression);
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> UseTaskFromResult(Document document, List<SyntaxNode> nullExpressions, INamedTypeSymbol taskTypeSymbol, INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        foreach (var nullExpression in nullExpressions)
        {
            var newExpression = generator.MemberAccessExpression(generator.TypeExpression(taskTypeSymbol), generator.GenericName("FromResult", typeSymbol.TypeArguments[0]));
            newExpression = generator.InvocationExpression(newExpression, generator.DefaultExpression(typeSymbol.TypeArguments[0]));
            editor.ReplaceNode(nullExpression, newExpression);
        }

        return editor.GetChangedDocument();
    }
}
