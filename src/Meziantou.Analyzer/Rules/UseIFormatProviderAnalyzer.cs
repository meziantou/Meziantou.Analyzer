using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseIFormatProviderParameter,
        title: "IFormatProvider is missing",
        messageFormat: "Use an overload of '{0}' that has a '{1}' parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

    private static readonly ConfigurationDefinition<bool> ExcludeToStringMethodsConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".exclude_tostring_methods", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> ConsiderNullableTypesConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".consider_nullable_types", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".treat_opaque_runtime_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> TreatUnsealedTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.UseIFormatProviderParameter + ".treat_unsealed_types_as_culture_sensitive", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext = new(compilation);
        private readonly OverloadFinder _overloadFinder = new(compilation);

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation is null)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            var options = GetOptions(context, operation);
            if (!CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, options), options))
                return;

            if (_cultureSensitiveContext.FormatProviderSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.FormatProviderSymbol))
            {
                if (operation.TargetMethod.Name == "ToString" && operation.Arguments.Length == 0 && operation.TargetMethod.ContainingType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: true), [_cultureSensitiveContext.FormatProviderSymbol]);
                if (overload is not null)
                {
                    if (CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, GetOptions(context, operation, unwrapNullableTypes: false)), options))
                    {
                        context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    }

                    return;
                }

                var targetMethodType = operation.TargetMethod.ContainingType;
                if (targetMethodType.IsNumberType() && _cultureSensitiveContext.NumberStyleSymbol is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(operation, options: default, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.NumberStyleSymbol]))
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var isDateTime = targetMethodType.IsDateTime() || targetMethodType.IsEqualToAny(_cultureSensitiveContext.DateTimeOffsetSymbol, _cultureSensitiveContext.DateOnlySymbol, _cultureSensitiveContext.TimeOnlySymbol);
                if (isDateTime)
                {
                    if (_cultureSensitiveContext.DateTimeStyleSymbol is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(operation, options: default, [_cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.DateTimeStyleSymbol]))
                    {
                        context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                        return;
                    }
                }

                if (operation.Arguments.IsEmpty && targetMethodType.Implements(_cultureSensitiveContext.SystemIFormattableSymbol) && _overloadFinder.HasOverloadWithAdditionalParameterOfType(operation, options: default, [_cultureSensitiveContext.FormatProviderSymbol, compilation.GetSpecialType(SpecialType.System_String)]))
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }
            }

            if (_cultureSensitiveContext.CultureInfoSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.CultureInfoSymbol))
            {
                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation, new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false), [_cultureSensitiveContext.CultureInfoSymbol]);
                if (overload is not null)
                {
                    if (CultureSensitiveFormattingContext.IsCultureSensitive(_cultureSensitiveContext.GetCultureSensitivity(operation, GetOptions(context, operation, unwrapNullableTypes: false)), options))
                    {
                        context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.CultureInfoSymbol.ToDisplayString());
                    }

                    return;
                }
            }
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (operation.TargetMethod.Name.EndsWith("OrDefault", StringComparison.Ordinal))
                return true;

            // ToString show culture-sensitive data by default
            if (operation.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, ExcludeToStringMethodsConfiguration);
            }

            return false;
        }

        private static CultureSensitiveOptions GetOptions(OperationAnalysisContext context, IOperation operation, bool? unwrapNullableTypes = null)
        {
            var options = CultureSensitiveOptions.None;
            var syntaxTree = operation.Syntax.SyntaxTree;

            if (unwrapNullableTypes ?? context.Options.GetConfigurationValue(syntaxTree, ConsiderNullableTypesConfiguration))
                options |= CultureSensitiveOptions.UnwrapNullableOfT;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

            if (context.Options.GetConfigurationValue(syntaxTree, TreatUnsealedTypesAsCultureSensitiveConfiguration))
                options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

            return options;
        }
    }
}
