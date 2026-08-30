namespace Meziantou.Analyzer.Internals;

internal static class SyntaxGeneratorExtensions
{
    /// <summary>
    /// Creates the expression accessing the member <paramref name="memberName"/> of the type <paramref name="type"/>.
    /// </summary>
    public static SyntaxNode TypeMemberAccessExpression(this SyntaxGenerator generator, ITypeSymbol type, string memberName, bool addImport = false)
    {
        var typeExpression = (TypeSyntax)generator.TypeExpression(type, addImport);
        return generator.MemberAccessExpression(typeExpression.AsExpressionSyntax(), memberName);
    }

    /// <summary>
    /// Rewrites the qualified names of a type syntax as member access expressions, so that it can be used where an
    /// expression is expected.
    /// </summary>
    /// <remarks>
    /// <see cref="SyntaxGenerator.TypeExpression(ITypeSymbol, bool)"/> returns a <see cref="QualifiedNameSyntax"/>,
    /// which the parser never produces where an expression is expected. Using it as the left side of a member access
    /// creates a syntax tree that does not match the one of the same code once parsed.
    /// </remarks>
    public static ExpressionSyntax AsExpressionSyntax(this TypeSyntax typeSyntax)
    {
        if (typeSyntax is not QualifiedNameSyntax qualifiedName)
            return typeSyntax;

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            qualifiedName.Left.AsExpressionSyntax(),
            qualifiedName.Right);

        return qualifiedName.CopyAnnotationsTo(memberAccess).WithTriviaFrom(qualifiedName);
    }
}
