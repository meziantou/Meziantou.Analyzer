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
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseAnOverloadThatHasMidpointRoundingFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAnOverloadThatHasMidpointRounding);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var invocationExpression = nodeToFix as InvocationExpressionSyntax ?? nodeToFix.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(invocationExpression, context.CancellationToken) is not IInvocationOperation invocationOperation)
            return;

        var midpointRoundingSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.MidpointRounding");
        if (midpointRoundingSymbol is null)
            return;

        if (!TryGetMidpointRoundingParameterInfo(semanticModel.Compilation, invocationOperation, midpointRoundingSymbol, out var parameterInfo))
            return;

        foreach (var midpointRoundingMember in midpointRoundingSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            if (midpointRoundingMember is { IsImplicitlyDeclared: true, Name: "value__" })
                continue;

            if (!midpointRoundingMember.HasConstantValue)
                continue;

            var midpointRoundingMemberName = midpointRoundingMember.Name;
            var title = "Add MidpointRounding." + midpointRoundingMemberName;
            var codeAction = CodeAction.Create(
                title,
                ct => AddMidpointRounding(context.Document, invocationExpression, parameterInfo, midpointRoundingSymbol, midpointRoundingMemberName, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static bool TryGetMidpointRoundingParameterInfo(Compilation compilation, IInvocationOperation invocationOperation, INamedTypeSymbol midpointRoundingSymbol, out AdditionalParameterInfo parameterInfo)
    {
        var overloadFinder = new OverloadFinder(compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(invocationOperation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true), [midpointRoundingSymbol]);
        if (overload is null)
        {
            parameterInfo = default;
            return false;
        }

        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            if (overload.Parameters[i].Type.IsEqualTo(midpointRoundingSymbol))
            {
                parameterInfo = new AdditionalParameterInfo(i, overload.Parameters[i].Name);
                return true;
            }
        }

        parameterInfo = default;
        return false;
    }

    private static async Task<Document> AddMidpointRounding(Document document, InvocationExpressionSyntax invocationExpression, AdditionalParameterInfo parameterInfo, INamedTypeSymbol midpointRoundingSymbol, string midpointRoundingMember, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var midpointRoundingExpression = generator.MemberAccessExpression(
            generator.TypeExpression(midpointRoundingSymbol, addImport: true),
            midpointRoundingMember);

        var newArgument = (ArgumentSyntax)generator.Argument(midpointRoundingExpression);

        InvocationExpressionSyntax newInvocation;
        if (parameterInfo.ParameterIndex > invocationExpression.ArgumentList.Arguments.Count)
        {
            var namedArgument = (ArgumentSyntax)generator.Argument(parameterInfo.ParameterName, RefKind.None, midpointRoundingExpression);
            var newArguments = invocationExpression.ArgumentList.Arguments.Add(namedArgument);
            newInvocation = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
        }
        else
        {
            var newArguments = invocationExpression.ArgumentList.Arguments.Insert(parameterInfo.ParameterIndex, newArgument);
            newInvocation = invocationExpression.WithArgumentList(SyntaxFactory.ArgumentList(newArguments));
        }

        editor.ReplaceNode(invocationExpression, newInvocation);
        return editor.GetChangedDocument();
    }

    private readonly record struct AdditionalParameterInfo(int ParameterIndex, string? ParameterName);
}
