using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseOperatingSystemInsteadOfRuntimeInformationFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseOperatingSystemInsteadOfRuntimeInformation);

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

        if (FindInvocation(semanticModel, nodeToFix, context.CancellationToken) is not { Arguments.Length: 1 } operation)
            return;

        if (operation.Arguments[0].Value is not IMemberReferenceOperation { Member.Name: var osPlatformName })
            return;

        var methodName = osPlatformName switch
        {
            "Windows" => "IsWindows",
            "Linux" => "IsLinux",
            "OSX" => "IsMacOS",
            "FreeBSD" => "IsFreeBSD",
            _ => null,
        };
        if (methodName is null)
            return;

        var operatingSystemType = semanticModel.Compilation.GetBestTypeByMetadataName("System.OperatingSystem");
        if (operatingSystemType is null)
            return;

        const string Title = "Use System.OperatingSystem";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseOperatingSystem(context.Document, operation.Syntax, methodName, operatingSystemType, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseOperatingSystem(Document document, SyntaxNode operationSyntax, string methodName, INamedTypeSymbol operatingSystemType, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var invocationExpression = editor.Generator.InvocationExpression(
            editor.Generator.MemberAccessExpression(editor.Generator.TypeExpression(operatingSystemType), methodName));

        editor.ReplaceNode(operationSyntax, invocationExpression.WithTriviaFrom(operationSyntax).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static IInvocationOperation? FindInvocation(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
    {
        foreach (var candidate in node.AncestorsAndSelf())
        {
            if (semanticModel.GetOperation(candidate, cancellationToken) is IInvocationOperation invocation)
                return invocation;
        }

        return null;
    }
}
