using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseCastAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseCast,
        title: "Do not use cast",
        messageFormat: "Do not use cast",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseCast));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        var operation = (IConversionOperation)context.Operation;

        // Only report cast expressions (explicit cast syntax like (int)value)
        if (operation.Syntax is not CastExpressionSyntax)
            return;

        // Do not report if it's a user-defined conversion
        if (operation.Conversion.IsUserDefined)
            return;

        context.ReportDiagnostic(Rule, operation);
    }
}
