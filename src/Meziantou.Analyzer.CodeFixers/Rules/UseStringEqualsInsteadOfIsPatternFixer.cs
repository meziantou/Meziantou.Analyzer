using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringEqualsInsteadOfIsPatternFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringEqualsInsteadOfIsPattern);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var isPatternExpression = nodeToFix as IsPatternExpressionSyntax ?? nodeToFix.AncestorsAndSelf().OfType<IsPatternExpressionSyntax>().FirstOrDefault();
        if (isPatternExpression is null)
            return;

        if (isPatternExpression.Pattern is not ConstantPatternSyntax { Expression: ExpressionSyntax constantExpression })
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var compilation = semanticModel.Compilation;
        if (compilation.GetBestTypeByMetadataName("System.StringComparison") is not { } stringComparisonType)
            return;

        if (semanticModel.GetOperation(isPatternExpression, context.CancellationToken) is not IIsPatternOperation { Value.Type: { } operandType })
            return;

        var operandKind = GetOperandKind(compilation, operandType, stringComparisonType);
        if (operandKind is OperandKind.None)
            return;

        RegisterCodeFix(nameof(StringComparison.Ordinal));
        RegisterCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        void RegisterCodeFix(string comparisonMode)
        {
            var title = "Use string.Equals " + comparisonMode;
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => ReplaceWithStringEquals(context.Document, isPatternExpression, constantExpression, operandKind, comparisonMode, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static OperandKind GetOperandKind(Compilation compilation, ITypeSymbol operandType, INamedTypeSymbol stringComparisonType)
    {
        if (operandType.SpecialType is SpecialType.System_String)
            return OperandKind.String;

        // The constant pattern also matches Span<char> and ReadOnlySpan<char> (C# 11). string.Equals does not accept
        // them, so the comparison is done by MemoryExtensions.Equals(ReadOnlySpan<char>, ReadOnlySpan<char>, StringComparison).
        if (operandType is INamedTypeSymbol { TypeArguments: [{ SpecialType: SpecialType.System_Char }] } namedType &&
            namedType.OriginalDefinition.IsEqualToAny(compilation.GetBestTypeByMetadataName("System.Span`1"), compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1")))
        {
            return HasMemoryExtensionsEquals(compilation, stringComparisonType) ? OperandKind.Span : OperandKind.None;
        }

        // Other operands (object, dynamic, interfaces implemented by string, type parameters) only match when their
        // value is a string, so they are converted using 'as string', which is null when the value is not a string.
        if (operandType.IsReferenceType || operandType is ITypeParameterSymbol { IsValueType: false })
            return OperandKind.Object;

        return OperandKind.None;
    }

    private static bool HasMemoryExtensionsEquals(Compilation compilation, INamedTypeSymbol stringComparisonType)
    {
        var memoryExtensionsType = compilation.GetBestTypeByMetadataName("System.MemoryExtensions");
        var readOnlySpanType = compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1");
        if (memoryExtensionsType is null || readOnlySpanType is null)
            return false;

        var readOnlySpanOfCharType = readOnlySpanType.Construct(compilation.GetSpecialType(SpecialType.System_Char));
        return memoryExtensionsType.GetMembers(nameof(MemoryExtensions.Equals))
            .OfType<IMethodSymbol>()
            .Any(method => method is { IsStatic: true, DeclaredAccessibility: Accessibility.Public, Parameters.Length: 3 } &&
                           method.Parameters[0].Type.IsEqualTo(readOnlySpanOfCharType) &&
                           method.Parameters[1].Type.IsEqualTo(readOnlySpanOfCharType) &&
                           method.Parameters[2].Type.IsEqualTo(stringComparisonType));
    }

    private static async Task<Document> ReplaceWithStringEquals(Document document, IsPatternExpressionSyntax isPatternExpression, ExpressionSyntax constantExpression, OperandKind operandKind, string comparisonMode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var compilation = editor.SemanticModel.Compilation;

        var stringComparisonType = compilation.GetBestTypeByMetadataName("System.StringComparison")!;
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        var newExpression = operandKind switch
        {
            OperandKind.Span => generator.InvocationExpression(
                generator.TypeMemberAccessExpression(compilation.GetBestTypeByMetadataName("System.MemoryExtensions")!, nameof(MemoryExtensions.Equals), addImport: true),
                isPatternExpression.Expression,
                constantExpression,
                generator.TypeMemberAccessExpression(stringComparisonType, comparisonMode, addImport: true)),

            OperandKind.Object => generator.InvocationExpression(
                generator.TypeMemberAccessExpression(stringType, nameof(string.Equals)),
                generator.TryCastExpression(isPatternExpression.Expression, generator.TypeExpression(stringType)),
                constantExpression,
                generator.TypeMemberAccessExpression(stringComparisonType, comparisonMode, addImport: true)),

            _ => generator.InvocationExpression(
                generator.TypeMemberAccessExpression(stringType, nameof(string.Equals)),
                isPatternExpression.Expression,
                constantExpression,
                generator.TypeMemberAccessExpression(stringComparisonType, comparisonMode, addImport: true)),
        };

        editor.ReplaceNode(isPatternExpression, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private enum OperandKind
    {
        None,
        String,
        Object,
        Span,
    }
}
