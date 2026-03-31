using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ValidateUnsafeAccessorAttributeUsageFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UnsafeAccessorAttribute_NameMustBeSet);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        const string title = "Set UnsafeAccessor Name";
        context.RegisterCodeFix(
            CodeAction.Create(title, ct => SetNameProperty(context.Document, nodeToFix, ct), equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> SetNameProperty(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var declaration = (SyntaxNode?)nodeToFix.FirstAncestorOrSelf<LocalFunctionStatementSyntax>() ?? nodeToFix.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (declaration is null)
            declaration = editor.OriginalRoot;

        foreach (var (attributeList, methodName) in EnumerateCandidateAttributes(declaration))
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (!IsUnsafeAccessorAttribute(attribute))
                    continue;

                if (HasNameProperty(attribute))
                    return document;

                if (methodName.Length == 0)
                    return document;

                var argument = AttributeArgument(
                    NameEquals(IdentifierName("Name")),
                    nameColon: null,
                    expression: LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(methodName)));

                var newArgumentList = attribute.ArgumentList is null
                    ? AttributeArgumentList(SeparatedList(new[] { argument }))
                    : attribute.ArgumentList.AddArguments(argument);
                var newAttribute = attribute.WithArgumentList(newArgumentList);

                editor.ReplaceNode(attribute, newAttribute.WithAdditionalAnnotations(Formatter.Annotation));
                return editor.GetChangedDocument();
            }
        }

        return document;
    }

    private static IEnumerable<(AttributeListSyntax AttributeList, string MethodName)> EnumerateCandidateAttributes(SyntaxNode declaration)
    {
        switch (declaration)
        {
            case LocalFunctionStatementSyntax localFunction:
                foreach (var attributeList in localFunction.AttributeLists)
                {
                    yield return (attributeList, localFunction.Identifier.ValueText);
                }

                yield break;

            case MethodDeclarationSyntax method:
                foreach (var attributeList in method.AttributeLists)
                {
                    yield return (attributeList, method.Identifier.ValueText);
                }

                foreach (var localFunctionDeclaration in method.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
                {
                    foreach (var attributeList in localFunctionDeclaration.AttributeLists)
                    {
                        yield return (attributeList, localFunctionDeclaration.Identifier.ValueText);
                    }
                }

                yield break;
        }

        foreach (var localFunctionDeclaration in declaration.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
        {
            foreach (var attributeList in localFunctionDeclaration.AttributeLists)
            {
                yield return (attributeList, localFunctionDeclaration.Identifier.ValueText);
            }
        }
    }

    private static bool HasNameProperty(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList is null)
            return false;

        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == "Name")
                return true;
        }

        return false;
    }

    private static bool IsUnsafeAccessorAttribute(AttributeSyntax attribute)
        => IsUnsafeAccessorAttributeName(attribute.Name);

    private static bool IsUnsafeAccessorAttributeName(NameSyntax name)
        => name switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText is "UnsafeAccessor" or "UnsafeAccessorAttribute",
            QualifiedNameSyntax qualified => IsUnsafeAccessorAttributeName(qualified.Right),
            AliasQualifiedNameSyntax aliasQualified => IsUnsafeAccessorAttributeName(aliasQualified.Name),
            _ => false,
        };
}
