using System;
using System.Collections.Immutable;
using System.ComponentModel.Design.Serialization;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseIFormatProviderParameter,
        title: "IFormatProvider is missing",
        messageFormat: "Use an overload of '{0}' that has a '{1}' parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext;

        public AnalyzerContext(Compilation compilation)
        {
            _cultureSensitiveContext = new CultureSensitiveFormattingContext(compilation);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation == null)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            if (!_cultureSensitiveContext.IsCultureSensitiveOperation(operation))
                return;

            if (_cultureSensitiveContext.FormatProviderSymbol != null && !operation.HasArgumentOfType(_cultureSensitiveContext.FormatProviderSymbol))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(operation, includeObsoleteMethods: false, _cultureSensitiveContext.FormatProviderSymbol);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                if (operation.TargetMethod.ContainingType.IsNumberType() && operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, _cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.NumberStyleSymbol))
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var isDateTime = operation.TargetMethod.ContainingType.IsDateTime() || operation.TargetMethod.ContainingType.IsEqualToAny(_cultureSensitiveContext.DateTimeOffsetSymbol, _cultureSensitiveContext.DateOnlySymbol, _cultureSensitiveContext.TimeOnlySymbol);
                if (isDateTime)
                {
                    if (operation.Arguments.Length >= 1 && !_cultureSensitiveContext.IsCultureSensitiveType(operation.TargetMethod.ContainingType, format: operation.Arguments[0].Value))
                        return;

                    if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, _cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.DateTimeStyleSymbol))
                    {
                        context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                        return;
                    }
                }
            }

            if (_cultureSensitiveContext.CultureInfoSymbol != null && !operation.HasArgumentOfType(_cultureSensitiveContext.CultureInfoSymbol))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(context.Compilation, includeObsoleteMethods: false, _cultureSensitiveContext.CultureInfoSymbol);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.CultureInfoSymbol.ToDisplayString());
                    return;
                }
            }
        }


        private static bool IsExcludedMethod(OperationAnalysisContext context, IOperation operation)
        {
            // ToString show culture-sensitive data by default
            if (operation?.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, "MA0011.exclude_tostring_methods", defaultValue: true);
            }

            return false;
        }
    }
}
