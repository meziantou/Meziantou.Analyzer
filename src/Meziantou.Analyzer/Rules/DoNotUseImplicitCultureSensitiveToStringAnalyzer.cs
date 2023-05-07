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
    private static readonly DiagnosticDescriptor s_stringConcatRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString,
        title: "Do not use implicit culture-sensitive ToString",
        messageFormat: "Do not use implicit culture-sensitive ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString));

    private static readonly DiagnosticDescriptor s_stringInterpolationRule = new(
        RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation,
        title: "Do not use implicit culture-sensitive ToString in interpolated strings",
        messageFormat: "Do not use implicit culture-sensitive ToString in interpolated strings",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation));

    private static readonly DiagnosticDescriptor s_objectToStringRule = new(
        RuleIdentifiers.DoNotUseCultureSensitiveObjectToString,
        title: "Do not use culture-sensitive object.ToString",
        messageFormat: "Do not use culture-sensitive object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseCultureSensitiveObjectToString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_stringConcatRule, s_stringInterpolationRule, s_objectToStringRule);

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

    private sealed class AnalyzerContext
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext;

        public AnalyzerContext(Compilation compilation)
        {
            _cultureSensitiveContext = new CultureSensitiveFormattingContext(compilation);
        }

        public static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (IsExcludedMethod(context, s_objectToStringRule, operation))
                return;

            if (operation.TargetMethod.Name == "ToString" && operation.TargetMethod.ContainingType.IsObject() && operation.TargetMethod.Parameters.Length == 0)
            {
                if (operation.Instance != null && operation.Instance.Type.IsObject())
                {
                    context.ReportDiagnostic(s_objectToStringRule, operation);
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

            if (IsExcludedMethod(context, s_stringConcatRule, operation))
                return;

            if (!IsNonCultureSensitiveOperand(context, s_stringConcatRule, operation.LeftOperand))
            {
                context.ReportDiagnostic(s_stringConcatRule, operation.LeftOperand);
            }

            if (!IsNonCultureSensitiveOperand(context, s_stringConcatRule, operation.RightOperand))
            {
                context.ReportDiagnostic(s_stringConcatRule, operation.RightOperand);
            }
        }

        public void AnalyzeInterpolatedString(OperationAnalysisContext context)
        {
            // Check if parent is InterpolatedString.Invariant($"") or conversion to string?
            var operation = (IInterpolatedStringOperation)context.Operation;

            if (operation.ConstantValue.HasValue)
                return;

            if (IsExcludedMethod(context, s_stringInterpolationRule, operation))
                return;

            var options = MustUnwrapNullableTypes(context, s_stringInterpolationRule, operation) ? CultureSensitiveOptions.UnwrapNullableOfT : CultureSensitiveOptions.None;

            var parent = operation.Parent;
            if (parent is IConversionOperation conversionOperation)
            {
                // `FormattableString _ = $""` is valid whereas `string _ = $""` may not be
                if (conversionOperation.Type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.FormattableString")))
                    return;
            }

            foreach (var part in operation.Parts.OfType<IInterpolationOperation>())
            {
                var expression = part.Expression;
                var type = expression.Type;
                if (expression == null || type == null)
                    continue;

                if (_cultureSensitiveContext.IsCultureSensitiveOperation(part, options | CultureSensitiveOptions.UseInvocationReturnType))
                {
                    context.ReportDiagnostic(s_stringInterpolationRule, part);
                }
            }
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation)
        {
            // ToString show culture-sensitive data by default
            if (operation?.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, descriptor.Id + ".exclude_tostring_methods", defaultValue: true);
            }

            return false;
        }

        private bool IsNonCultureSensitiveOperand(OperationAnalysisContext context, DiagnosticDescriptor rule, IOperation operand)
        {
            // Implicit conversion from a type number
            if (operand is null)
                return true;

            if (operand is IConversionOperation conversion && conversion.IsImplicit && conversion.Type.IsObject() && conversion.Operand.Type != null)
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
            if (rule == s_stringConcatRule)
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString);
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, RuleIdentifiers.DoNotUseImplicitCultureSensitiveToString + ".consider_nullable_types", defaultValue: true);
            }
            else if (rule == s_stringInterpolationRule)
            {
                Debug.Assert(rule.Id == RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation);
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, RuleIdentifiers.DoNotUseImplicitCultureSensitiveToStringInterpolation + ".consider_nullable_types", defaultValue: true);
            }

            return false;
        }
    }
}
