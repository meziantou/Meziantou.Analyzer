using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseZeroToInitializeAnEnumValue : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue,
            title: "Use Explicit enum value instead of 0",
            messageFormat: "Use Explicit enum value for '{0}' instead of 0",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseZeroToInitializeAnEnumValue));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeSimpleAssignment, OperationKind.SimpleAssignment);
            context.RegisterOperationAction(AnalyzeVariableInitializer, OperationKind.VariableInitializer);
            context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
        }

        private void AnalyzeArgument(OperationAnalysisContext context)
        {
            var operation = (IArgumentOperation)context.Operation;
            if (operation.IsImplicit)
                return;

            if (operation.Value is IConversionOperation conversionOperation)
            {
                ValidateConversionOperation(context, conversionOperation);
            }
        }

        private static void AnalyzeSimpleAssignment(OperationAnalysisContext context)
        {
            var operation = (ISimpleAssignmentOperation)context.Operation;
            if (operation.Value is IConversionOperation conversionOperation)
            {
                ValidateConversionOperation(context, conversionOperation);
            }
        }

        private static void AnalyzeVariableInitializer(OperationAnalysisContext context)
        {
            var operation = (IVariableInitializerOperation)context.Operation;
            if (operation.Value is IConversionOperation conversionOperation)
            {
                ValidateConversionOperation(context, conversionOperation);
            }
        }

        private static void ValidateConversionOperation(OperationAnalysisContext context, IConversionOperation operation)
        {
            if (!operation.IsImplicit)
                return;

            if (!operation.Type.IsEnumeration())
                return;

            // Skip "default" keyword
            if (operation.Operand is ILiteralOperation && operation.Operand.ConstantValue.HasValue && operation.Operand.ConstantValue.Value is int i && i == 0)
            {
                context.ReportDiagnostic(s_rule, operation, operation.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat));
            }
        }
    }
}
