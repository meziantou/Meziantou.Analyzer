using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class EqualityShouldBeCorrectlyImplementedFixer : CodeFixProvider
{
    private static readonly ImmutableArray<string> ComparisonOperatorNames = ImmutableArray.Create(
        "op_LessThan",
        "op_LessThanOrEqual",
        "op_GreaterThan",
        "op_GreaterThanOrEqual",
        "op_Equality",
        "op_Inequality");

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT,
        RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT,
        RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject,
        RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT,
        RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (nodeToFix is null)
            return;

        foreach (var diagnosticId in context.Diagnostics.Select(diagnostic => diagnostic.Id).Distinct(StringComparer.Ordinal))
        {
            switch (diagnosticId)
            {
                case RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT:
                    RegisterCodeFix(context, context.Document, nodeToFix, "Implement System.IEquatable", ImplementIEquatable, diagnosticId);
                    break;

                case RuleIdentifiers.ClassWithCompareToTShouldImplementIComparableT:
                    RegisterCodeFix(context, context.Document, nodeToFix, "Implement System.IComparable", ImplementIComparable, diagnosticId);
                    break;

                case RuleIdentifiers.ClassWithEqualsTShouldOverrideEqualsObject:
                    RegisterCodeFix(context, context.Document, nodeToFix, "Override Equals(object)", OverrideEqualsObject, diagnosticId);
                    break;

                case RuleIdentifiers.ClassImplementingIComparableTShouldImplementIEquatableT:
                    RegisterCodeFix(context, context.Document, nodeToFix, "Implement System.IEquatable", ImplementIEquatableForComparable, diagnosticId);
                    break;

                case RuleIdentifiers.TheComparisonOperatorsShouldBeOverriddenWhenImplementingIComparable:
                    RegisterCodeFix(context, context.Document, nodeToFix, "Add comparison operators", AddComparisonOperators, diagnosticId);
                    break;
            }
        }
    }

    private static void RegisterCodeFix(CodeFixContext context, Document document, TypeDeclarationSyntax nodeToFix, string title, Func<Document, TypeDeclarationSyntax, CancellationToken, Task<Document>> action, string diagnosticId)
    {
        context.RegisterCodeFix(
            CodeAction.Create(title, ct => action(document, nodeToFix, ct), equivalenceKey: $"{title}_{diagnosticId}"),
            context.Diagnostics.Where(diagnostic => diagnostic.Id == diagnosticId));
    }

    private static async Task<Document> ImplementIComparable(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        return await AddInterface(document, nodeToFix, "System.IComparable`1", null, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> ImplementIEquatableForComparable(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null || semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: cancellationToken) is not INamedTypeSymbol declaredTypeSymbol)
            return document;

        var genericInterfaceSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");
        if (genericInterfaceSymbol is null)
            return document;

        var implementedInterface = genericInterfaceSymbol.Construct(
            ImmutableArray.Create<ITypeSymbol>(declaredTypeSymbol),
            ImmutableArray.Create(NullableAnnotation.None));
        var shouldAddInterface = !declaredTypeSymbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.IsEqualTo(implementedInterface));

        var hasEqualsMethod = declaredTypeSymbol.GetMembers().OfType<IMethodSymbol>().Any(EqualityShouldBeCorrectlyImplementedAnalyzerCommon.IsEqualsOfTMethod);
        if (!shouldAddInterface && hasEqualsMethod)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (shouldAddInterface)
        {
            var concreteInterfaceTypeNode = editor.Generator.TypeExpression(implementedInterface);
            editor.AddInterfaceType(nodeToFix, concreteInterfaceTypeNode.WithoutTrailingTrivia());
        }

        if (!hasEqualsMethod)
        {
            var fullyQualifiedTypeName = declaredTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var typeSyntax = ParseTypeName(fullyQualifiedTypeName).WithAdditionalAnnotations(Simplifier.Annotation);
            var equalsMethod = MethodDeclaration(
                PredefinedType(Token(SyntaxKind.BoolKeyword)),
                Identifier(nameof(object.Equals)))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("other")).WithType(typeSyntax))))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ParseExpression($"((global::System.IComparable<{fullyQualifiedTypeName}>)this).CompareTo(other) == 0")
                            .WithAdditionalAnnotations(Simplifier.Annotation)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation);

            editor.AddMember(nodeToFix, equalsMethod);
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> ImplementIEquatable(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: cancellationToken) is not INamedTypeSymbol declaredTypeSymbol)
            return document;

        var genericInterfaceSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");
        if (genericInterfaceSymbol is null)
            return document;

        var equalsMethod = declaredTypeSymbol.GetMembers().OfType<IMethodSymbol>().SingleOrDefault(EqualityShouldBeCorrectlyImplementedAnalyzerCommon.IsEqualsOfTMethod);
        if (equalsMethod is null)
            return document;

        var nullableAnnotation = equalsMethod.Parameters[0].NullableAnnotation;
        var implementedInterface = genericInterfaceSymbol.Construct(
            ImmutableArray.Create<ITypeSymbol>(declaredTypeSymbol),
            ImmutableArray.Create(nullableAnnotation));
        if (declaredTypeSymbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.IsEqualTo(implementedInterface)))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var concreteInterfaceTypeNode = editor.Generator.TypeExpression(implementedInterface);
        editor.AddInterfaceType(nodeToFix, concreteInterfaceTypeNode.WithoutTrailingTrivia());
        return editor.GetChangedDocument();
    }

    private static async Task<Document> AddInterface(Document document, TypeDeclarationSyntax nodeToFix, string metadataName, NullableAnnotation? nullableAnnotation, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null || semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: cancellationToken) is not INamedTypeSymbol declaredTypeSymbol)
            return document;

        var genericInterfaceSymbol = semanticModel.Compilation.GetBestTypeByMetadataName(metadataName);
        if (genericInterfaceSymbol is null)
            return document;

        INamedTypeSymbol implementedInterface;
        if (nullableAnnotation is not null)
        {
            implementedInterface = genericInterfaceSymbol.Construct(
                ImmutableArray.Create<ITypeSymbol>(declaredTypeSymbol),
                ImmutableArray.Create(nullableAnnotation.Value));
        }
        else
        {
            implementedInterface = genericInterfaceSymbol.Construct(declaredTypeSymbol);
        }

        if (declaredTypeSymbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.IsEqualTo(implementedInterface)))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var concreteInterfaceTypeNode = editor.Generator.TypeExpression(implementedInterface);
        editor.AddInterfaceType(nodeToFix, concreteInterfaceTypeNode.WithoutTrailingTrivia());
        return editor.GetChangedDocument();
    }

    private static async Task<Document> OverrideEqualsObject(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null || semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: cancellationToken) is not INamedTypeSymbol declaredTypeSymbol)
            return document;

        if (declaredTypeSymbol.GetMembers().OfType<IMethodSymbol>().Any(IsEqualsObjectOverride))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var fullyQualifiedTypeName = declaredTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeSyntax = ParseTypeName(fullyQualifiedTypeName).WithAdditionalAnnotations(Simplifier.Annotation);
        var equalsMethod = MethodDeclaration(
            PredefinedType(Token(SyntaxKind.BoolKeyword)),
            Identifier(nameof(object.Equals)))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(ParameterList(
                SingletonSeparatedList(
                    Parameter(Identifier("obj")).WithType(PredefinedType(Token(SyntaxKind.ObjectKeyword))))))
            .WithExpressionBody(
                ArrowExpressionClause(
                    BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        IsPatternExpression(
                            IdentifierName("obj"),
                            DeclarationPattern(typeSyntax.WithoutTrivia(), SingleVariableDesignation(Identifier("other")))),
                        ParseExpression($"((global::System.IEquatable<{fullyQualifiedTypeName}>)this).Equals(other)")
                            .WithAdditionalAnnotations(Simplifier.Annotation))))
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.AddMember(nodeToFix, equalsMethod);
        return editor.GetChangedDocument();
    }

    private static bool IsEqualsObjectOverride(IMethodSymbol symbol)
    {
        return symbol.Name == nameof(object.Equals) &&
               symbol.ReturnType.IsBoolean() &&
               symbol.Parameters.Length == 1 &&
               symbol.Parameters[0].Type.IsObject() &&
               symbol.DeclaredAccessibility == Accessibility.Public &&
               !symbol.IsStatic &&
               symbol.IsOverride;
    }

    private static async Task<Document> AddComparisonOperators(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null || semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: cancellationToken) is not INamedTypeSymbol declaredTypeSymbol)
            return document;

        var missingOperators = new HashSet<string>(ComparisonOperatorNames, StringComparer.Ordinal);
        foreach (var method in declaredTypeSymbol.GetAllMembers().OfType<IMethodSymbol>())
        {
            if (method.MethodKind is MethodKind.UserDefinedOperator)
            {
                missingOperators.Remove(method.Name);
            }
        }

        if (missingOperators.Count == 0)
            return document;

        var fullyQualifiedTypeName = declaredTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeSyntax = ParseTypeName(fullyQualifiedTypeName).WithAdditionalAnnotations(Simplifier.Annotation);
        var compareExpression = $"global::System.Collections.Generic.Comparer<{fullyQualifiedTypeName}>.Default.Compare(left, right)";
        var equalsExpression = $"global::System.Collections.Generic.EqualityComparer<{fullyQualifiedTypeName}>.Default.Equals(left, right)";

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        AddMissingOperator("op_LessThan", "<", compareExpression + " < 0");
        AddMissingOperator("op_LessThanOrEqual", "<=", compareExpression + " <= 0");
        AddMissingOperator("op_GreaterThan", ">", compareExpression + " > 0");
        AddMissingOperator("op_GreaterThanOrEqual", ">=", compareExpression + " >= 0");
        AddMissingOperator("op_Equality", "==", equalsExpression);
        AddMissingOperator("op_Inequality", "!=", "!(" + equalsExpression + ")");

        return editor.GetChangedDocument();

        void AddMissingOperator(string operatorName, string operatorToken, string bodyExpression)
        {
            if (!missingOperators.Contains(operatorName))
                return;

            var method = OperatorDeclaration(
                PredefinedType(Token(SyntaxKind.BoolKeyword)),
                Token(GetOperatorSyntaxKind(operatorToken)))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(
                    ParameterList(
                        SeparatedList(
                        [
                            Parameter(Identifier("left")).WithType(typeSyntax),
                            Parameter(Identifier("right")).WithType(typeSyntax),
                        ])))
                .WithExpressionBody(ArrowExpressionClause(ParseExpression(bodyExpression).WithAdditionalAnnotations(Simplifier.Annotation)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation);
            editor.AddMember(nodeToFix, method);
        }
    }

    private static SyntaxKind GetOperatorSyntaxKind(string operatorToken)
    {
        return operatorToken switch
        {
            "<" => SyntaxKind.LessThanToken,
            "<=" => SyntaxKind.LessThanEqualsToken,
            ">" => SyntaxKind.GreaterThanToken,
            ">=" => SyntaxKind.GreaterThanEqualsToken,
            "==" => SyntaxKind.EqualsEqualsToken,
            "!=" => SyntaxKind.ExclamationEqualsToken,
            _ => throw new ArgumentOutOfRangeException(nameof(operatorToken)),
        };
    }
}
