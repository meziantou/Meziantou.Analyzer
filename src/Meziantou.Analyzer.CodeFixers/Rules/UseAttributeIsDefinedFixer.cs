using System.Collections.Immutable;
using System.Composition;
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
public sealed class UseAttributeIsDefinedFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseAttributeIsDefined);

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

        var title = "Use Attribute.IsDefined";
        var codeAction = CodeAction.Create(
            title,
            ct => ReplaceWithAttributeIsDefined(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> ReplaceWithAttributeIsDefined(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var operation = semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        SyntaxNode? replacement = null;

        if (operation is IBinaryOperation binaryOperation)
        {
            var negate = binaryOperation.OperatorKind == BinaryOperatorKind.Equals;
            var invocation = GetGetCustomAttributeInvocation(binaryOperation.LeftOperand) ?? GetGetCustomAttributeInvocation(binaryOperation.RightOperand);
            if (invocation is not null)
            {
                replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, invocation, negate);
            }
        }
        else if (operation is IIsPatternOperation isPatternOperation)
        {
            var negate = isPatternOperation.Pattern is INegatedPatternOperation;
            var invocation = GetGetCustomAttributeInvocation(isPatternOperation.Value);
            if (invocation is not null)
            {
                replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, invocation, negate);
            }
        }
        else if (operation is IInvocationOperation invocationOperation && invocationOperation.TargetMethod.Name == "Any")
        {
            if (invocationOperation.Arguments.Length == 1 && invocationOperation.Arguments[0].Value is IInvocationOperation getCustomAttributesInvocation)
            {
                replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, getCustomAttributesInvocation, negate: false);
            }
        }

        if (replacement is not null)
        {
            editor.ReplaceNode(nodeToFix, replacement.WithTriviaFrom(nodeToFix));
            return editor.GetChangedDocument();
        }

        return document;
    }

    private static IInvocationOperation? GetGetCustomAttributeInvocation(IOperation operation)
    {
        if (operation is IInvocationOperation invocation &&
            (invocation.TargetMethod.Name == "GetCustomAttribute" || invocation.TargetMethod.Name == "GetCustomAttributes"))
        {
            return invocation;
        }

        return null;
    }

    private static SyntaxNode CreateAttributeIsDefinedInvocation(SyntaxGenerator generator, SemanticModel semanticModel, IInvocationOperation invocation, bool negate)
    {
        var attributeType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Attribute");
        var attributeTypeSyntax = generator.TypeExpression(attributeType!);

        var instance = invocation.Instance;
        var instanceSyntax = instance?.Syntax;

        var arguments = new List<SyntaxNode>();
        if (instanceSyntax is not null)
        {
            arguments.Add(instanceSyntax);
        }

        if (invocation.Arguments.Length > 0)
        {
            foreach (var arg in invocation.Arguments)
            {
                arguments.Add(arg.Syntax);
            }
        }

        var isDefinedInvocation = generator.InvocationExpression(
            generator.MemberAccessExpression(attributeTypeSyntax, "IsDefined"),
            arguments);

        if (negate)
        {
            return generator.LogicalNotExpression(isDefinedInvocation);
        }

        return isDefinedInvocation;
    }
}
