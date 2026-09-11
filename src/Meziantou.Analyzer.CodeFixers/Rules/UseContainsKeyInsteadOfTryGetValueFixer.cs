using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseContainsKeyInsteadOfTryGetValueFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseContainsKeyInsteadOfTryGetValue);

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

        if (FindInvocation(semanticModel, nodeToFix, context.CancellationToken) is not { Arguments.Length: 2 } operation)
            return;

        if (operation.TargetMethod.Name != "TryGetValue")
            return;

        if (operation.Arguments[1].Value is not IDiscardOperation)
            return;

        if (operation.Syntax is not InvocationExpressionSyntax invocationSyntax ||
            invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var generator = SyntaxGenerator.GetGenerator(context.Document);
        if (CreateContainsKeyInvocation(generator, semanticModel, operation, invocationSyntax, memberAccess) is not { } newInvocation)
            return;

        const string Title = "Use ContainsKey";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseContainsKey(context.Document, invocationSyntax, newInvocation, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Document> UseContainsKey(Document document, InvocationExpressionSyntax invocationSyntax, InvocationExpressionSyntax newInvocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(invocationSyntax, newInvocation.WithTriviaFrom(invocationSyntax).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static InvocationExpressionSyntax? CreateContainsKeyInvocation(SyntaxGenerator generator, SemanticModel semanticModel, IInvocationOperation operation, InvocationExpressionSyntax invocationSyntax, MemberAccessExpressionSyntax memberAccess)
    {
        if (operation.Instance?.Type is not { } receiverType)
            return null;

        var containsKeyMethods = GetContainsKeyMethods(semanticModel.Compilation, operation.TargetMethod);
        if (containsKeyMethods.Count == 0)
            return null;

        var argumentList = ArgumentList(SingletonSeparatedList(invocationSyntax.ArgumentList.Arguments[0]));
        var newInvocation = invocationSyntax
            .WithExpression(memberAccess.WithName(IdentifierName("ContainsKey")))
            .WithArgumentList(argumentList);
        if (IsContainsKeyInvocation(newInvocation))
            return newInvocation;

        // ContainsKey is not accessible from the receiver type, e.g. when it is implemented explicitly, so call it through the interface.
        // Casting a value type would box it, and "base" cannot be cast.
        if (!receiverType.IsReferenceType || memberAccess.Expression is BaseExpressionSyntax)
            return null;

        var interfaceType = generator.TypeExpression(containsKeyMethods[0].ContainingType, addImport: true).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        var castInvocation = InvocationExpression(
            (ExpressionSyntax)generator.MemberAccessExpression(generator.CastExpression(interfaceType, memberAccess.Expression.WithoutLeadingTrivia()), "ContainsKey"),
            argumentList);
        if (IsContainsKeyInvocation(castInvocation))
            return castInvocation;

        return null;

        bool IsContainsKeyInvocation(ExpressionSyntax expression)
        {
            if (semanticModel.GetSpeculativeSymbolInfo(invocationSyntax.SpanStart, expression, SpeculativeBindingOption.BindAsExpression).Symbol is not IMethodSymbol method)
                return false;

            foreach (var containsKey in containsKeyMethods)
            {
                if (method.IsEqualTo(containsKey) || method.IsEqualTo(receiverType.FindImplementationForInterfaceMember(containsKey)))
                    return true;
            }

            return false;
        }
    }

    private static List<IMethodSymbol> GetContainsKeyMethods(Compilation compilation, IMethodSymbol tryGetValueMethod)
    {
        var result = new List<IMethodSymbol>();
        foreach (var metadataName in (ReadOnlySpan<string>)["System.Collections.Generic.IReadOnlyDictionary`2", "System.Collections.Generic.IDictionary`2"])
        {
            var symbol = compilation.GetBestTypeByMetadataName(metadataName);
            if (symbol is null)
                continue;

            var containingType = tryGetValueMethod.ContainingType;
            var iface = containingType.OriginalDefinition.IsEqualTo(symbol) ? containingType : containingType.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.IsEqualTo(symbol));
            if (iface is null)
                continue;

            if (iface.GetMembers("TryGetValue").FirstOrDefault() is not IMethodSymbol tryGetValue)
                continue;

            if (!tryGetValueMethod.IsEqualTo(tryGetValue) && !tryGetValueMethod.IsEqualTo(containingType.FindImplementationForInterfaceMember(tryGetValue)))
                continue;

            if (iface.GetMembers("ContainsKey").OfType<IMethodSymbol>().FirstOrDefault(m => m.Parameters.Length == 1) is { } containsKey)
            {
                result.Add(containsKey);
            }
        }

        return result;
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
