using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
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

            // Handles: ref T local = ref span[0]
            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);

            // Handles: return ref span[0]
            compilationContext.RegisterOperationAction(analyzerContext.AnalyzeReturn, OperationKind.Return);

            // Handles: Method(ref span[0]), Method(in span[0])
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

        public void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var declarator = (IVariableDeclaratorOperation)context.Operation;
            if (!declarator.Symbol.IsRef)
                return;

            var initValue = declarator.Initializer?.Value.UnwrapConversionOperations();
            if (initValue is null)
                return;

            ReportIfMatch(context, initValue);
        }

        public void AnalyzeReturn(OperationAnalysisContext context)
        {
            var returnsByRef = context.ContainingSymbol switch
            {
                IMethodSymbol method => method.ReturnsByRef || method.ReturnsByRefReadonly,
                IPropertySymbol property => property.ReturnsByRef || property.ReturnsByRefReadonly,
                _ => false,
            };
            if (!returnsByRef)
                return;

            var returnedValue = ((IReturnOperation)context.Operation).ReturnedValue?.UnwrapConversionOperations();
            if (returnedValue is null)
                return;

            ReportIfMatch(context, returnedValue);
        }

        public void AnalyzeArgument(OperationAnalysisContext context)
        {
            var argument = (IArgumentOperation)context.Operation;

            var refKind = argument.Parameter?.RefKind ?? RefKind.None;
            if (refKind is RefKind.None or RefKind.Out)
                return;

            ReportIfMatch(context, argument.Value.UnwrapConversionOperations());
        }

        private void ReportIfMatch(OperationAnalysisContext context, IOperation value)
        {
            IOperation? receiverOp;
            IOperation? indexOp;

            if (value is IArrayElementReferenceOperation { Indices: [var arrayIdx], ArrayReference: var arrayRef })
            {
                receiverOp = arrayRef;
                indexOp = arrayIdx;
            }
            else if (value is IPropertyReferenceOperation { Property.IsIndexer: true, Arguments: [{ Value: var propIdx }], Instance: var instance })
            {
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

            return type.OriginalDefinition.IsEqualToAny(_spanType, _readOnlySpanType);
        }
    }
}
