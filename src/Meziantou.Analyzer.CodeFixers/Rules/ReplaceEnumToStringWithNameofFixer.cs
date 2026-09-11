namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ReplaceEnumToStringWithNameofFixer : CodeFixProvider
{
    private const string Title = "Use nameof";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReplaceEnumToStringWithNameof);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var operation = semanticModel.GetOperation(nodeToFix, context.CancellationToken);
        var enumMember = operation switch
        {
            IInvocationOperation invocation => invocation.Instance,
            IInterpolationOperation interpolation => interpolation.Expression,
            _ => null,
        };

        if (enumMember is not IFieldReferenceOperation { Field: var field, Syntax: ExpressionSyntax enumMemberSyntax })
            return;

        var aliases = GetMembersWithSameValue(field);
        if (aliases.Length <= 1)
        {
            context.RegisterCodeFix(CodeAction.Create(Title, ct => UseNameof(context.Document, nodeToFix, enumMemberSyntax, ct), equivalenceKey: Title), context.Diagnostics);
            return;
        }

        if (enumMemberSyntax is not (MemberAccessExpressionSyntax or IdentifierNameSyntax))
            return;

        // Enum.ToString formats the value, so when several members share the same value, it may return the name of another member
        // than the one used in the source code. The runtime returns the first declared member, but this is not documented.
        // So, the first declared member is the default fix, and the other members are provided as alternative fixes.
        var generator = SyntaxGenerator.GetGenerator(context.Document);
        for (var i = 0; i < aliases.Length; i++)
        {
            var alias = aliases[i];
            var title = $"Use nameof({field.ContainingType.Name}.{alias.Name})";
            var aliasSyntax = ReplaceMemberName(generator, enumMemberSyntax, alias.Name);
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseNameof(context.Document, nodeToFix, aliasSyntax, ct), equivalenceKey: i == 0 ? Title : title), context.Diagnostics);
        }
    }

    private static ImmutableArray<IFieldSymbol> GetMembersWithSameValue(IFieldSymbol field)
    {
        if (!field.HasConstantValue)
            return ImmutableArray.Create(field);

        return field.ContainingType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.HasConstantValue && Equals(member.ConstantValue, field.ConstantValue))
            .ToImmutableArray();
    }

    private static ExpressionSyntax ReplaceMemberName(SyntaxGenerator generator, ExpressionSyntax enumMemberSyntax, string name)
    {
        var newName = (IdentifierNameSyntax)generator.IdentifierName(name);
        return enumMemberSyntax switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.WithName(newName.WithTriviaFrom(memberAccess.Name)),
            _ => newName.WithTriviaFrom(enumMemberSyntax),
        };
    }

    private static async Task<Document> UseNameof(Document document, SyntaxNode nodeToFix, ExpressionSyntax enumMemberSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var newExpression = (ExpressionSyntax)editor.Generator.NameOfExpression(enumMemberSyntax);
        if (nodeToFix is InterpolationSyntax)
        {
            editor.ReplaceNode(nodeToFix, SyntaxFactory.Interpolation(newExpression));
        }
        else
        {
            editor.ReplaceNode(nodeToFix, newExpression);
        }

        return editor.GetChangedDocument();
    }
}
