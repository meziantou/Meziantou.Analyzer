using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseEqualityOperatorsForSpanOfCharAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DoNotUseEqualityOperatorsForSpanOfChar,
        title: "Use SequenceEqual instead of equality operator",
        messageFormat: "Use SequenceEqual instead of {0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseEqualityOperatorsForSpanOfChar));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var spanOfString = ctx.Compilation.GetTypeByMetadataName("System.Span`1")?.Construct(ctx.Compilation.GetSpecialType(SpecialType.System_Char));
            var readOnlySpanOfString = ctx.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1")?.Construct(ctx.Compilation.GetSpecialType(SpecialType.System_Char));
            if (spanOfString is null && readOnlySpanOfString is null)
                return;

            var analyzerContext = new AnalyzerContext(spanOfString, readOnlySpanOfString);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeBinaryOperator, OperationKind.BinaryOperator);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly INamedTypeSymbol?[] _spanTypes;

        public AnalyzerContext(params INamedTypeSymbol?[] spanTypes)
        {
            _spanTypes = spanTypes;
        }

        public void AnalyzeBinaryOperator(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind == BinaryOperatorKind.Equals ||
                operation.OperatorKind == BinaryOperatorKind.NotEquals)
            {
                if (!IsSpanOfString(operation.LeftOperand.Type) || !IsSpanOfString(operation.RightOperand.Type))
                    return;

                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (operation.IsInExpressionArgument())
                    return;

                context.ReportDiagnostic(s_rule, operation, $"{operation.OperatorKind} operator");
            }
        }

        private bool IsSpanOfString(ITypeSymbol? symbol)
        {
            return symbol != null && symbol.IsEqualToAny(_spanTypes);
        }
    }
}
