using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseMemoryMarshalGetReferenceForEmptyBuffersFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseMemoryMarshalGetReferenceForEmptyBuffers);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not ElementAccessExpressionSyntax elementAccess)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var receiverType = semanticModel.GetTypeInfo(elementAccess.Expression, context.CancellationToken).Type;
        if (receiverType is null)
            return;

        var memoryMarshalType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Runtime.InteropServices.MemoryMarshal");
        if (memoryMarshalType is null)
            return;

        string methodName;
        if (receiverType.TypeKind is TypeKind.Array)
        {
            // GetArrayDataReference is available since .NET 6; verify it exists before offering the fix
            if (!HasGetArrayDataReference(memoryMarshalType))
                return;

            methodName = "GetArrayDataReference";
        }
        else
        {
            methodName = "GetReference";
        }

        var title = $"Use MemoryMarshal.{methodName}";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => FixAsync(context.Document, elementAccess, memoryMarshalType, methodName, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static bool HasGetArrayDataReference(INamedTypeSymbol memoryMarshalType)
    {
        foreach (var member in memoryMarshalType.GetMembers("GetArrayDataReference"))
        {
            if (member is IMethodSymbol)
                return true;
        }

        return false;
    }

    private static async Task<Document> FixAsync(
        Document document,
        ElementAccessExpressionSyntax elementAccess,
        INamedTypeSymbol memoryMarshalType,
        string methodName,
        CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var receiverExpression = elementAccess.Expression;

        var replacement = generator.InvocationExpression(
            generator.MemberAccessExpression(
                generator.TypeExpression(memoryMarshalType),
                methodName),
            receiverExpression.WithoutTrivia())
            .WithTriviaFrom(elementAccess);

        editor.ReplaceNode(elementAccess, replacement);
        return editor.GetChangedDocument();
    }
}
