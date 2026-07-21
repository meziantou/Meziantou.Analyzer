using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseMemoryMarshalGetReferenceForEmptyBuffersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseMemoryMarshalGetReferenceForEmptyBuffers,
        title: "Use MemoryMarshal.GetReference instead of indexing at 0",
        messageFormat: "Use MemoryMarshal.GetReference instead of indexing at 0, which throws on empty buffers",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Indexing a Span<T>, ReadOnlySpan<T>, or array at index 0 to obtain a by-reference value throws IndexOutOfRangeException on empty buffers. Use MemoryMarshal.GetReference (for spans) or MemoryMarshal.GetArrayDataReference (for arrays) instead, which safely returns a reference to the start even for empty collections.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseMemoryMarshalGetReferenceForEmptyBuffers));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var analyzerContext = new AnalyzerContext(compilationContext.Compilation);
            if (!analyzerContext.IsValid)
                return;

            // Handles: ref span[0], ref array[0] used in ref-return, ref-local, etc.
            compilationContext.RegisterSyntaxNodeAction(analyzerContext.AnalyzeRefExpression, SyntaxKind.RefExpression);

            // Handles: Method(ref span[0]), Method(in span[0]), Method(ref readonly span[0])
            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeArgument, OperationKind.Argument);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly INamedTypeSymbol? _spanType;
        private readonly INamedTypeSymbol? _readOnlySpanType;

        public AnalyzerContext(Compilation compilation)
        {
            _spanType = compilation.GetBestTypeByMetadataName("System.Span`1");
            _readOnlySpanType = compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1");
        }

        public bool IsValid => _spanType is not null || _readOnlySpanType is not null;

        public void AnalyzeRefExpression(SyntaxNodeAnalysisContext context)
        {
            var refExpression = (RefExpressionSyntax)context.Node;

            if (refExpression.Expression is not ElementAccessExpressionSyntax elementAccess)
                return;

            if (!IsIndexZero(context, elementAccess.ArgumentList))
                return;

            var receiverTypeInfo = context.SemanticModel.GetTypeInfo(elementAccess.Expression, context.CancellationToken);
            if (!IsSpanOrArray(receiverTypeInfo.Type))
                return;

            context.ReportDiagnostic(Rule, elementAccess);
        }

        public void AnalyzeArgument(OperationAnalysisContext context)
        {
            var argument = (IArgumentOperation)context.Operation;

            if (argument.Syntax is not ArgumentSyntax { RefKindKeyword: var refKindToken })
                return;

            // Only care about by-reference arguments (ref, in, ref readonly)
            if (refKindToken.IsKind(SyntaxKind.None))
                return;

            var value = argument.Value.UnwrapConversionOperations();

            IOperation? receiverOp;
            IOperation? indexOp;

            if (value is IArrayElementReferenceOperation { Indices: [var arrayIdx], ArrayReference: var arrayRef })
            {
                // True array: T[]
                receiverOp = arrayRef;
                indexOp = arrayIdx;
            }
            else if (value is IPropertyReferenceOperation { Property.IsIndexer: true, Arguments: [{ Value: var propIdx }], Instance: var instance })
            {
                // Span<T> / ReadOnlySpan<T> indexer
                receiverOp = instance;
                indexOp = propIdx;
            }
            else
            {
                return;
            }

            if (!indexOp.IsConstantZero())
                return;

            if (!IsSpanOrArray(receiverOp?.Type))
                return;

            if (value.Syntax is not ElementAccessExpressionSyntax elementAccessSyntax)
                return;

            context.ReportDiagnostic(Rule, elementAccessSyntax);
        }

        private bool IsSpanOrArray(ITypeSymbol? type)
        {
            if (type is null)
                return false;

            if (type.TypeKind is TypeKind.Array)
                return true;

            if (type.OriginalDefinition.IsEqualToAny(_spanType, _readOnlySpanType))
                return true;

            return false;
        }

        private static bool IsIndexZero(SyntaxNodeAnalysisContext context, BracketedArgumentListSyntax argumentList)
        {
            if (argumentList.Arguments.Count != 1)
                return false;

            var argument = argumentList.Arguments[0];
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
                return false;

            var constantValue = context.SemanticModel.GetConstantValue(argument.Expression, context.CancellationToken);
            return constantValue.HasValue && constantValue.Value is 0;
        }
    }
}
