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
            else
            {
                // Check for GetCustomAttributes().Length comparisons
                var lengthInvocation = GetGetCustomAttributesLengthInvocation(binaryOperation.LeftOperand, out var isLeftSide);
                if (lengthInvocation is null)
                {
                    lengthInvocation = GetGetCustomAttributesLengthInvocation(binaryOperation.RightOperand, out isLeftSide);
                    isLeftSide = !isLeftSide; // If found on right, flip the comparison perspective
                }

                if (lengthInvocation is not null)
                {
                    var negateLength = ShouldNegateLengthComparison(binaryOperation, isLeftSide);
                    replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, lengthInvocation, negateLength);
                }
                else
                {
                    // Check for GetCustomAttributes().Count() comparisons
                    var countInvocation = GetGetCustomAttributesCountInvocation(semanticModel, binaryOperation.LeftOperand, out var foundOnLeft);
                    var countIsOnLeft = foundOnLeft;
                    if (countInvocation is null)
                    {
                        countInvocation = GetGetCustomAttributesCountInvocation(semanticModel, binaryOperation.RightOperand, out var foundOnRight);
                        if (foundOnRight)
                        {
                            countIsOnLeft = false; // Count is on right side
                        }
                    }

                    if (countInvocation is not null)
                    {
                        var negateCount = ShouldNegateLengthComparison(binaryOperation, countIsOnLeft);
                        replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, countInvocation, negateCount);
                    }
                }
            }
        }
        else if (operation is IIsPatternOperation isPatternOperation)
        {
            var negate = isPatternOperation.Pattern is IConstantPatternOperation;
            var invocation = GetGetCustomAttributeInvocation(isPatternOperation.Value);
            if (invocation is not null)
            {
                replacement = CreateAttributeIsDefinedInvocation(generator, semanticModel, invocation, negate);
            }
        }
        else if (operation is IInvocationOperation invocationOperation)
        {
            // Check if this is the Any<T>(IEnumerable<T>) method
            var enumerableAnyMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:System.Linq.Enumerable.Any``1(System.Collections.Generic.IEnumerable{``0})", semanticModel.Compilation) as IMethodSymbol;
            if (SymbolEqualityComparer.Default.Equals(invocationOperation.TargetMethod.OriginalDefinition, enumerableAnyMethod) &&
                invocationOperation.Arguments.Length == 1 &&
                invocationOperation.Arguments[0].Value is IInvocationOperation getCustomAttributesInvocation)
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

    private static IInvocationOperation? GetGetCustomAttributesLengthInvocation(IOperation operation, out bool isFound)
    {
        isFound = false;
        if (operation is IPropertyReferenceOperation propertyReference &&
            propertyReference.Property.Name == "Length" &&
            propertyReference.Instance is IInvocationOperation invocation &&
            invocation.TargetMethod.Name == "GetCustomAttributes")
        {
            isFound = true;
            return invocation;
        }

        return null;
    }

    private static IInvocationOperation? GetGetCustomAttributesCountInvocation(SemanticModel semanticModel, IOperation operation, out bool isFound)
    {
        isFound = false;
        if (operation is not IInvocationOperation countInvocation)
            return null;

        // Check if this is the specific Count<T>(IEnumerable<T>) method
        var enumerableCountMethod = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:System.Linq.Enumerable.Count``1(System.Collections.Generic.IEnumerable{``0})", semanticModel.Compilation) as IMethodSymbol;
        if (!SymbolEqualityComparer.Default.Equals(countInvocation.TargetMethod.OriginalDefinition, enumerableCountMethod))
            return null;

        if (countInvocation.Arguments.Length != 1)
            return null;

        if (countInvocation.Arguments[0].Value is not IInvocationOperation invocation)
            return null;

        if (invocation.TargetMethod.Name != "GetCustomAttributes")
            return null;

        isFound = true;
        return invocation;
    }

    private static bool ShouldNegateLengthComparison(IBinaryOperation operation, bool lengthIsOnLeft)
    {
        var otherOperand = lengthIsOnLeft ? operation.RightOperand : operation.LeftOperand;
        if (otherOperand.ConstantValue is not { HasValue: true, Value: int value })
            return false;

        // Determine if we should negate based on operator and compared value
        // Patterns that mean "has attributes" -> IsDefined (no negation):
        //   length > 0, length >= 1, length != 0, 0 < length, 1 <= length, 0 != length
        // Patterns that mean "no attributes" -> !IsDefined (negate):
        //   length == 0, length <= 0, length < 1, 0 == length, 0 >= length, 1 > length
        
        if (lengthIsOnLeft)
        {
            return operation.OperatorKind switch
            {
                BinaryOperatorKind.Equals when value == 0 => true,                    // length == 0 -> !IsDefined
                BinaryOperatorKind.NotEquals when value == 0 => false,                // length != 0 -> IsDefined
                BinaryOperatorKind.GreaterThan when value == 0 => false,              // length > 0 -> IsDefined
                BinaryOperatorKind.GreaterThanOrEqual when value == 1 => false,       // length >= 1 -> IsDefined
                BinaryOperatorKind.LessThan when value == 1 => true,                  // length < 1 -> !IsDefined
                BinaryOperatorKind.LessThanOrEqual when value == 0 => true,           // length <= 0 -> !IsDefined
                _ => false,
            };
        }
        else
        {
            // When length is on the right: reverse the operator logic
            return operation.OperatorKind switch
            {
                BinaryOperatorKind.Equals when value == 0 => true,                    // 0 == length -> !IsDefined
                BinaryOperatorKind.NotEquals when value == 0 => false,                // 0 != length -> IsDefined
                BinaryOperatorKind.LessThan when value == 0 => false,                 // 0 < length (length > 0) -> IsDefined
                BinaryOperatorKind.LessThanOrEqual when value == 1 => false,          // 1 <= length (length >= 1) -> IsDefined
                BinaryOperatorKind.GreaterThan when value == 1 => true,               // 1 > length (length < 1) -> !IsDefined
                BinaryOperatorKind.GreaterThanOrEqual when value == 0 => true,        // 0 >= length (length <= 0) -> !IsDefined
                _ => false,
            };
        }
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

        foreach (var arg in invocation.Arguments)
        {
            arguments.Add(arg.Syntax);
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
