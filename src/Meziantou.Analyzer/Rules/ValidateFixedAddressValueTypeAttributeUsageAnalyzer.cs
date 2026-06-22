using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValidateFixedAddressValueTypeAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor FieldMustBeStaticRule = new(
        RuleIdentifiers.FixedAddressValueTypeAttribute_FieldMustBeStatic,
        title: "[FixedAddressValueType] fields must be static",
        messageFormat: "[FixedAddressValueType] can only be applied to static fields",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FixedAddressValueTypeAttribute_FieldMustBeStatic));

    private static readonly DiagnosticDescriptor FieldTypeMustBeValueTypeRule = new(
        RuleIdentifiers.FixedAddressValueTypeAttribute_FieldTypeMustBeValueType,
        title: "[FixedAddressValueType] fields must be value types",
        messageFormat: "[FixedAddressValueType] can only be applied to fields of value type",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FixedAddressValueTypeAttribute_FieldTypeMustBeValueType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(FieldMustBeStaticRule, FieldTypeMustBeValueTypeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var attributeType = compilationContext.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.FixedAddressValueTypeAttribute");
            if (attributeType is null)
                return;

            compilationContext.RegisterSymbolAction(context => Analyze(context, attributeType), SymbolKind.Field);
        });
    }

    private static void Analyze(SymbolAnalysisContext context, ITypeSymbol attributeType)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.GetAttribute(attributeType, inherits: false) is null)
            return;

        if (!field.IsStatic)
        {
            context.ReportDiagnostic(FieldMustBeStaticRule, field);
        }

        if (!field.Type.IsValueType)
        {
            context.ReportDiagnostic(FieldTypeMustBeValueTypeRule, field, DiagnosticFieldReportOptions.ReportOnReturnType);
        }
    }
}
