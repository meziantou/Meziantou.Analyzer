using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DoNotUseZeroToInitializeAnEnumValue : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue,
        title: "Use Explicit enum value instead of 0",
        messageFormat: "Use Explicit enum value for '{0}' instead of 0",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        var operation = (IConversionOperation)context.Operation;
        if (!operation.IsImplicit)
            return;

        if (operation.Type is not INamedTypeSymbol { EnumUnderlyingType: not null and var enumType })
            return;

        if (operation.Operand is IDefaultValueOperation)
            return;

        if (operation.Parent is IArgumentOperation { IsImplicit: true })
            return;

#if !ROSLYN4_5_OR_GREATER
        if (operation.Syntax.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute))
            return;
#endif

        if (operation.ConstantValue is { HasValue: true, Value: not null and var value } && IsZero(enumType, value))
        {
            context.ReportDiagnostic(Rule, operation, operation.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
        }

        static bool IsZero(ITypeSymbol enumType, object value)
        {
            return enumType.SpecialType switch
            {
                SpecialType.System_SByte => value is sbyte converted && converted == 0,
                SpecialType.System_Byte => value is byte converted && converted == 0,
                SpecialType.System_Int16 => value is short converted && converted == 0,
                SpecialType.System_UInt16 => value is ushort converted && converted == 0,
                SpecialType.System_Int32 => value is int converted && converted == 0,
                SpecialType.System_UInt32 => value is uint converted && converted == 0u,
                SpecialType.System_Int64 => value is long converted && converted == 0L,
                SpecialType.System_UInt64 => value is ulong converted && converted == 0uL,
                _ => false,
            };
        }
    }
}
