namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class RemoveUselessToStringFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.RemoveUselessToString);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } syntax)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(syntax, context.CancellationToken) is not IInvocationOperation operation)
            return;

        // The arguments are evaluated even though string.ToString ignores them, so they can only be removed
        // along with the call when evaluating them has no observable effect
        var cultureInfoType = semanticModel.Compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
        foreach (var argument in operation.Arguments)
        {
            if (!CanBeDiscarded(argument.Value, cultureInfoType))
                return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Remove ToString",
                ct => Remove(context.Document, syntax, memberAccess, ct),
                equivalenceKey: "Remove ToString"),
            context.Diagnostics);
    }

    /// <summary>
    /// Indicates whether removing the expression from the code is safe, i.e. evaluating it has no observable side effect.
    /// </summary>
    private static bool CanBeDiscarded(IOperation operation, INamedTypeSymbol? cultureInfoType)
    {
        return operation switch
        {
            IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion => CanBeDiscarded(conversion.Operand, cultureInfoType),
            IParenthesizedOperation parenthesized => CanBeDiscarded(parenthesized.Operand, cultureInfoType),
            ILiteralOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation => true,
            IFieldReferenceOperation fieldReference => fieldReference.Field.IsStatic || fieldReference.Instance is IInstanceReferenceOperation,
            // The static properties of CultureInfo, such as InvariantCulture, have no observable side effect
            IPropertyReferenceOperation { Instance: null } propertyReference => propertyReference.Property.ContainingType.IsEqualTo(cultureInfoType),
            _ => operation.ConstantValue.HasValue,
        };
    }

    private static async Task<Document> Remove(Document document, InvocationExpressionSyntax expression, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(expression, memberAccess.Expression);
        return editor.GetChangedDocument();
    }
}
