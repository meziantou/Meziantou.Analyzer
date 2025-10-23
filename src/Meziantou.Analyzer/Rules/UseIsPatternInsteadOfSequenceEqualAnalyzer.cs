using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIsPatternInsteadOfSequenceEqualAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseIsPatternInsteadOfSequenceEqual,
        title: "Use 'is' operator instead of SequenceEqual",
        messageFormat: "Use 'is' operator instead of '{0}'",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIsPatternInsteadOfSequenceEqual));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var memoryExtensionsSymbol = compilation.GetBestTypeByMetadataName("System.MemoryExtensions");
            var spanCharSymbol = compilation.GetBestTypeByMetadataName("System.Span`1")?.Construct(compilation.GetSpecialType(SpecialType.System_Char));
            var readOnlySpanCharSymbol = compilation.GetBestTypeByMetadataName("System.ReadOnlySpan`1")?.Construct(compilation.GetSpecialType(SpecialType.System_Char));
            var stringComparisonSymbol = compilation.GetBestTypeByMetadataName("System.StringComparison");
            if (memoryExtensionsSymbol is null || spanCharSymbol is null || readOnlySpanCharSymbol is null || stringComparisonSymbol is null)
                return;

            var syntax = compilation.SyntaxTrees.FirstOrDefault();
            if (syntax is null || !syntax.GetCSharpLanguageVersion().IsCSharp11OrAbove())
                return;

            context.RegisterOperationAction(ctx => AnalyzeInvocation(ctx, spanCharSymbol, readOnlySpanCharSymbol, memoryExtensionsSymbol, stringComparisonSymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol spanCharSymbol, INamedTypeSymbol readOnlySpanCharSymbol, INamedTypeSymbol memoryExtensionsSymbol, INamedTypeSymbol stringComparisonSymbol)
    {
        var operation = (IInvocationOperation)context.Operation;
        var method = operation.TargetMethod;
        if (!method.ContainingType.IsEqualTo(memoryExtensionsSymbol))
            return;

        if (method.Name is "SequenceEqual" && method.Parameters.Length == 2 && method.Parameters[0].Type.IsEqualToAny(readOnlySpanCharSymbol, spanCharSymbol))
        {
            if (IsConstantValue(operation.Arguments[1].Value))
            {
                context.ReportDiagnostic(Rule, operation, method.Name);
            }
        }
        else if (method.Name is "Equals" && method.Parameters.Length == 3 && method.Parameters[0].Type.IsEqualTo(readOnlySpanCharSymbol))
        {
            if (IsConstantValue(operation.Arguments[1].Value) && IsStringComparisonOrdinal(operation.Arguments[2].Value, stringComparisonSymbol))
            {
                context.ReportDiagnostic(Rule, operation, method.Name);
            }
        }

        static bool IsConstantValue(IOperation operation)
        {
            operation = operation.UnwrapImplicitConversionOperations();
            return operation is { ConstantValue: { HasValue: true, Value: string } };
        }

        static bool IsStringComparisonOrdinal(IOperation operation, INamedTypeSymbol stringComparisonSymbol)
        {
            if (!operation.Type.IsEqualTo(stringComparisonSymbol))
                return false;

            return operation.ConstantValue is { HasValue: true, Value: (int)StringComparison.Ordinal };
        }
    }
}
