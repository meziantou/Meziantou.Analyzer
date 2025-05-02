using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseEqualsMethodInsteadOfOperatorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseEqualsMethodInsteadOfOperator,
        title: "Use Equals method instead of operator",
        messageFormat: "Use Equals method instead of == or != operator",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEqualsMethodInsteadOfOperator));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.GetSpecialType(SpecialType.System_Object).GetMembers("Equals").FirstOrDefault() is not IMethodSymbol objectEqualsSymbol)
                return;

            context.RegisterOperationAction(context => AnalyzerBinaryOperation(context, objectEqualsSymbol), OperationKind.Binary);
        });
    }

    private static void AnalyzerBinaryOperation(OperationAnalysisContext context, IMethodSymbol objectEqualsSymbol)
    {
        var operation = (IBinaryOperation)context.Operation;
        if (operation is { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals, OperatorMethod: null })
        {
            if (IsNull(operation.LeftOperand) || IsNull(operation.RightOperand))
                return;

            var leftType = operation.LeftOperand.UnwrapImplicitConversionOperations().Type;
            if (operation.IsLifted)
            {
                leftType = leftType.GetUnderlyingNullableTypeOrSelf();
            }

            if (leftType is null)
                return;

            if (leftType.IsValueType)
                return;

            switch (leftType.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_DateTime:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Enum:
                case SpecialType.System_Object:
                case SpecialType.System_String:
                    return;
            }

            // Check if the type have an Equals method
            var overrideEqualsSymbol = leftType.GetMembers("Equals").OfType<IMethodSymbol>().FirstOrDefault(m => m.IsOrOverrideMethod(objectEqualsSymbol));
            if (overrideEqualsSymbol is not null)
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }

    public static bool IsNull(IOperation operation)
        => operation.UnwrapConversionOperations() is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
}
