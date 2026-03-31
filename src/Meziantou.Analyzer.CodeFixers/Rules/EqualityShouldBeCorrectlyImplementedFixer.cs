using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class EqualityShouldBeCorrectlyImplementedFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT);

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

        if (semanticModel.GetDeclaredSymbol(nodeToFix, cancellationToken: context.CancellationToken) is not ITypeSymbol declaredTypeSymbol)
            return;

        var genericInterfaceSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.IEquatable`1");
        if (genericInterfaceSymbol is null)
            return;

        var equalsMethod = declaredTypeSymbol.GetMembers().OfType<IMethodSymbol>().SingleOrDefault(m => EqualityShouldBeCorrectlyImplementedAnalyzerCommon.IsEqualsOfTMethod(m) && m is not null);
        if (equalsMethod is null)
            return;

        var nullableAnnotation = equalsMethod.Parameters[0].NullableAnnotation;

        var title = "Implement System.IEquatable";
        var codeAction = CodeAction.Create(
            title,
            ct => ImplementIEquatable(context.Document, nodeToFix, genericInterfaceSymbol, declaredTypeSymbol, nullableAnnotation, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> ImplementIEquatable(Document document, SyntaxNode nodeToFix, INamedTypeSymbol genericInterfaceSymbol, ITypeSymbol declaredTypeSymbol, NullableAnnotation nullableAnnotation, CancellationToken cancellationToken)
    {
        var concreteInterfaceSymbol = genericInterfaceSymbol.Construct(
            ImmutableArray.Create(declaredTypeSymbol),
            ImmutableArray.Create(nullableAnnotation));

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var concreteInterfaceTypeNode = generator.TypeExpression(concreteInterfaceSymbol);

        editor.AddInterfaceType(nodeToFix, concreteInterfaceTypeNode.WithoutTrailingTrivia());

        return editor.GetChangedDocument();
    }
}
