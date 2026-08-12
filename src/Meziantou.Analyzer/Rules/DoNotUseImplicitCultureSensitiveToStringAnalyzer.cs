using System.Collections.Immutable;
using System.Diagnostics;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseImplicitCultureSensitiveToStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor StringConcatRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString,
        title: "Do not use implicit culture-sensitive ToString",
        messageFormat: "Do not use implicit culture-sensitive ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString));

    private static readonly DiagnosticDescriptor StringInterpolationRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation,
        title: "Do not use implicit culture-sensitive ToString in interpolated strings",
        messageFormat: "Do not use implicit culture-sensitive ToString in interpolated strings",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation));

    private static readonly DiagnosticDescriptor ObjectToStringRule = new(
        RuleIdentifiers.DoNotUseCultureSensitiveObjectToString,
        title: "Do not use object.ToString",
        messageFormat: "Do not use object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseCultureSensitiveObjectToString));

    private static readonly ConfigurationDefinition<bool> StringConcatConsiderNullableTypesConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString + ".consider_nullable_types", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> StringInterpolationConsiderNullableTypesConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation + ".consider_nullable_types", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> StringConcatTreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString + ".treat_opaque_runtime_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> StringInterpolationTreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation + ".treat_opaque_runtime_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> StringConcatTreatUnsealedTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString + ".treat_unsealed_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> StringInterpolationTreatUnsealedTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation + ".treat_unsealed_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> ExcludeToStringMethodsConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString + ".exclude_tostring_methods", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> ExcludeToStringMethodsInterpolationConfiguration = new(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation + ".exclude_tostring_methods", defaultValue: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(StringConcatRule, StringInterpolationRule, ObjectToStringRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);

            context.RegisterOperationAction(analyzerContext.AnalyzeBinaryOperation, OperationKind.Binary);
            context.RegisterOperationAction(analyzerContext.AnalyzeInterpolatedString, OperationKind.InterpolatedString);
            context.RegisterOperationAction(AnalyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext = new(compilation);


        public static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (IsExcludedMethod(context, ExcludeToStringMethodsConfiguration, operation))
                return;

            if (operation.TargetMethod.Name == "ToString" && operation.TargetMethod.ContainingType.IsObject() && operation.TargetMethod.Parameters.Length == 0)
            {
                if (operation.Instance is not null && operation.Instance.Type.IsObject())
                {
                    context.ReportDiagnostic(ObjectToStringRule, operation);
                }
            }
        }

        public void AnalyzeBinaryOperation(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind != BinaryOperatorKind.Add)
                return;

            if (!operation.Type.IsString())
                return;

            if (operation.ConstantValue.HasValue)
                return;

            if (IsExcludedMethod(context, ExcludeToStringMethodsConfiguration, operation))
                return;

            if (ShouldReportCultureSensitiveOperand(context, StringConcatRule, operation.LeftOperand))
            {
                context.ReportDiagnostic(StringConcatRule, operation.LeftOperand);
            }

            if (ShouldReportCultureSensitiveOperand(context, StringConcatRule, operation.RightOperand))
            {
                context.ReportDiagnostic(StringConcatRule, operation.RightOperand);
            }
        }

        public void AnalyzeInterpolatedString(OperationAnalysisContext context)
        {
            // Check if parent is InterpolatedString.Invariant($"") or conversion to string?
            var operation = (IInterpolatedStringOperation)context.Operation;

            if (operation.ConstantValue.HasValue)
                return;

            if (IsExcludedMethod(context, ExcludeToStringMethodsInterpolationConfiguration, operation))
                return;

            if (_cultureSensitiveContext.IsInInterpolatedStringHandlerContext(operation))
                return;

            var options = GetOptions(context, StringInterpolationRule, operation);

            var parent = operation.Parent;
            if (parent is IConversionOperation conversionOperation)
            {
                // `FormattableString _ = $""` is valid whereas `string _ = $""` may not be
                if (conversionOperation.Type.IsEqualTo(_cultureSensitiveContext.FormattableStringSymbol))
                    return;
            }

            foreach (var part in operation.Parts.OfType<IInterpolationOperation>())
            {
                var expression = part.Expression;
                var type = expression.Type;
                if (expression is null || type is null)
                    continue;

                if (CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(part, options | CultureSensitiveOptions.UseInvocationReturnType), options))
                {
                    context.ReportDiagnostic(StringInterpolationRule, part);
                }

                if (CultureSensitiveFormattingContext.UsesObjectToString(type))
                {
                    context.ReportDiagnostic(ObjectToStringRule, expression);
                }
            }
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, ConfigurationDefinition<bool> configuration, IOperation operation)
        {
            // ToString show culture-sensitive data by default
            if (operation?.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, configuration);
            }

            return false;
        }

        private bool ShouldReportCultureSensitiveOperand(OperationAnalysisContext context, DiagnosticDescriptor rule, IOperation operand)
        {
            if (operand is null)
                return false;

            // Interpolated strings are analyzed by MA0076.
            if (operand is IInterpolatedStringOperation)
                return false;

            // String concatenation inserts an implicit conversion to object for many value types.
            // Analyze the value before this conversion, but preserve explicitly object-typed values.
            var value = operand is IConversionOperation { IsImplicit: true, Type: { SpecialType: SpecialType.System_Object }, Operand.Type: not null } conversion
                ? conversion.Operand
                : operand;
            var options = GetOptions(context, rule, operand);

            return CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(value, options | CultureSensitiveOptions.UseInvocationReturnType), options);
        }

        private static CultureSensitiveOptions GetOptions(OperationAnalysisContext context, DiagnosticDescriptor rule, IOperation operation)
        {
            var options = CultureSensitiveOptions.None;

            // Avoid an allocation when creating the key
            if (StringConcatRule.Equals(rule))
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString);
                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringConcatConsiderNullableTypesConfiguration))
                    options |= CultureSensitiveOptions.UnwrapNullableOfT;

                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringConcatTreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
                    options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringConcatTreatUnsealedTypesAsCultureSensitiveConfiguration))
                    options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

                return options;
            }
            else if (StringInterpolationRule.Equals(rule))
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation);
                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringInterpolationConsiderNullableTypesConfiguration))
                    options |= CultureSensitiveOptions.UnwrapNullableOfT;

                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringInterpolationTreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
                    options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

                if (context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringInterpolationTreatUnsealedTypesAsCultureSensitiveConfiguration))
                    options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

                return options;
            }

            return options;
        }
    }
}
