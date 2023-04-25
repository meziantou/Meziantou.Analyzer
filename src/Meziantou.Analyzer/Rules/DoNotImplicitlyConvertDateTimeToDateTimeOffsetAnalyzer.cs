using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotImplicitlyConvertDateTimeToDateTimeOffsetAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_ruleImplicitConversion = new(
        RuleIdentifiers.DoNotImplicitlyConvertDateTimeToDateTimeOffset,
        title: "Do not convert implicitly to DateTimeOffset",
        messageFormat: "Do not convert implicitly to DateTimeOffset",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotImplicitlyConvertDateTimeToDateTimeOffset));

    private static readonly DiagnosticDescriptor s_ruleUseDateTimeOffset = new(
        RuleIdentifiers.UseDateTimeOffsetInsteadOfDateTime,
        title: "Use DateTimeOffset instead of relying on the implicit conversion",
        messageFormat: "Use DateTimeOffset instead of relying on the implicit conversion",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDateTimeOffsetInsteadOfDateTime));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_ruleImplicitConversion, s_ruleUseDateTimeOffset);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var datetimeOffsetSymbol = context.Compilation.GetBestTypeByMetadataName("System.DateTimeOffset");
            if (datetimeOffsetSymbol == null)
                return;

            context.RegisterOperationAction(context => AnalyzeConversion(context, datetimeOffsetSymbol), OperationKind.Conversion);
        });
    }

    private static void AnalyzeConversion(OperationAnalysisContext context, INamedTypeSymbol dateTimeOffsetSymbol)
    {
        var operation = (IConversionOperation)context.Operation;
        if (!operation.Conversion.IsImplicit)
            return;

        if (operation.Type.IsEqualTo(dateTimeOffsetSymbol) && operation.Operand.Type.IsDateTime())
        {
            // DateTime.Now and DateTime.UtcNow set the DateTime.Kind, so the conversion result is well-known
            if (operation.Operand is IMemberReferenceOperation { Member.Name: "UtcNow" or "Now", Member.ContainingType.SpecialType: SpecialType.System_DateTime })
            {
                context.ReportDiagnostic(s_ruleUseDateTimeOffset, operation);
            }
            else
            {
                context.ReportDiagnostic(s_ruleImplicitConversion, operation);
            }
        }
    }
}
