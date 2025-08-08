using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BothSideOfTheConditionAreIdenticalAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.BothSideOfTheConditionAreIdentical,
        title: "Both sides of the logical operation are identical",
        messageFormat: "Both sides of the logical operation are identical",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.BothSideOfTheConditionAreIdentical));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);


        context.RegisterOperationAction(AnalyzeBinaryOperation, OperationKind.Binary);
        context.RegisterOperationAction(AnalyzeBinaryPatternOperation, OperationKind.BinaryPattern);
    }

    private void AnalyzeBinaryOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.And or BinaryOperatorKind.Or or BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals)
        {
            if (operation.Type.IsBoolean() && operation.LeftOperand.Syntax.IsEquivalentTo(operation.RightOperand.Syntax, topLevel: false))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }

    private void AnalyzeBinaryPatternOperation(OperationAnalysisContext context)
    {
        var operation = (IBinaryPatternOperation)context.Operation;
        if (operation.OperatorKind is BinaryOperatorKind.And or BinaryOperatorKind.Or)
        {
            if (operation.LeftPattern.Syntax.IsEquivalentTo(operation.RightPattern.Syntax, topLevel: false))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }
}
