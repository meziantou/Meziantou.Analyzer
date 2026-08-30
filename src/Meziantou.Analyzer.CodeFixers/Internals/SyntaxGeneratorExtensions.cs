namespace Meziantou.Analyzer.Internals;

internal static class SyntaxGeneratorExtensions
{
    /// <summary>
    /// Creates the expression accessing the member <paramref name="memberName"/> of the type <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="SyntaxGenerator.TypeExpression(ITypeSymbol, bool)"/> returns a <see cref="QualifiedNameSyntax"/>,
    /// which the parser never produces where an expression is expected. Using it as the left side of a member access
    /// creates a syntax tree that does not match the one of the same code once parsed.
    /// </remarks>
    public static SyntaxNode TypeMemberAccessExpression(this SyntaxGenerator generator, ITypeSymbol type, string memberName, bool addImport = false)
    {
        var typeExpression = generator.TypeExpression(type, addImport);
        return generator.MemberAccessExpression(AsExpression(typeExpression), memberName);
    }

    private static SyntaxNode AsExpression(SyntaxNode node)
    {
        if (node is not QualifiedNameSyntax qualifiedName)
            return node;

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            (ExpressionSyntax)AsExpression(qualifiedName.Left),
            qualifiedName.Right);

        return qualifiedName.CopyAnnotationsTo(memberAccess).WithTriviaFrom(qualifiedName);
    }
}
