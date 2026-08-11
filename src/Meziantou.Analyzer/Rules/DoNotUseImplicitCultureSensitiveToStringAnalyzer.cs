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

            if (!IsNonCultureSensitiveOperand(context, StringConcatRule, operation.LeftOperand))
            {
                context.ReportDiagnostic(StringConcatRule, operation.LeftOperand);
            }

            if (!IsNonCultureSensitiveOperand(context, StringConcatRule, operation.RightOperand))
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

            var options = MustUnwrapNullableTypes(context, StringInterpolationRule, operation) ? CultureSensitiveOptions.UnwrapNullableOfT : CultureSensitiveOptions.None;

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

                if (_cultureSensitiveContext.IsCultureSensitiveOperation(part, options | CultureSensitiveOptions.UseInvocationReturnType))
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

        private bool IsNonCultureSensitiveOperand(OperationAnalysisContext context, DiagnosticDescriptor rule, IOperation operand)
        {
            // Implicit conversion from a type number
            if (operand is null)
                return true;

            if (operand is IConversionOperation conversion && conversion.IsImplicit && conversion.Type.IsObject() && conversion.Operand.Type is not null)
            {
                var value = conversion.Operand;
                var options = MustUnwrapNullableTypes(context, rule, operand) ? CultureSensitiveOptions.UnwrapNullableOfT : CultureSensitiveOptions.None;
                if (_cultureSensitiveContext.IsCultureSensitiveOperation(value, options | CultureSensitiveOptions.UseInvocationReturnType))
                    return false;
            }

            return true;
        }

        private static bool MustUnwrapNullableTypes(OperationAnalysisContext context, DiagnosticDescriptor rule, IOperation operation)
        {
            // Avoid an allocation when creating the key
            if (StringConcatRule.Equals(rule))
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString);
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringConcatConsiderNullableTypesConfiguration);
            }
            else if (StringInterpolationRule.Equals(rule))
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation);
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, StringInterpolationConsiderNullableTypesConfiguration);
            }

            return false;
        }
    }
}
