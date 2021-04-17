using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
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

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var formatProviderType = context.Compilation.GetTypeByMetadataName("System.IFormatProvider");
            var cultureInfoType = context.Compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
            var numberStyleType = context.Compilation.GetTypeByMetadataName("System.Globalization.NumberStyles");
            var dateTimeStyleType = context.Compilation.GetTypeByMetadataName("System.Globalization.DateTimeStyles");

            var operation = (IInvocationOperation)context.Operation;
            if (operation == null)
                return;

            var methodName = operation.TargetMethod.Name;
            if (string.Equals(methodName, "ToString", StringComparison.Ordinal))
            {
                // Boolean.ToString(IFormatProvider) should not be used
                if (operation.TargetMethod.ContainingType.IsBoolean())
                    return;

                // Char.ToString(IFormatProvider) should not be used
                if (operation.TargetMethod.ContainingType.IsChar())
                    return;

                // Guid.ToString(IFormatProvider) should not be used
                if (operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.Guid")))
                    return;

                // Enum.ToString(IFormatProvider) should not be used
                if (operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.Enum")))
                    return;

                // DateTime.ToString() or DateTimeOffset.ToString() with invariant formats (o, O, r, R, s, u)
                if (operation.Arguments.Length == 1 && operation.TargetMethod.ContainingType.IsEqualToAny(context.Compilation.GetTypeByMetadataName("System.DateTime"), context.Compilation.GetTypeByMetadataName("System.DateTimeOffset")))
                {
                    if (IsInvariantDateTimeFormat(operation.Arguments[0].Value))
                        return;
                }
            }

            if (formatProviderType != null && !operation.HasArgumentOfType(formatProviderType))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(operation, includeObsoleteMethods: false, formatProviderType);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, formatProviderType.ToDisplayString());
                    return;
                }

                if (operation.TargetMethod.ContainingType.IsNumberType() && operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, formatProviderType, numberStyleType))
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, formatProviderType.ToDisplayString());
                    return;
                }

                var isDateTime = operation.TargetMethod.ContainingType.IsDateTime() || operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.DateTimeOffset"));
                if (isDateTime)
                {
                    if (operation.Arguments.Length >= 1 && IsInvariantDateTimeFormat(operation.Arguments[0].Value))
                        return;

                    if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, formatProviderType, dateTimeStyleType))
                    {
                        context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, formatProviderType.ToDisplayString());
                        return;
                    }
                }
            }

            if (cultureInfoType != null && !operation.HasArgumentOfType(cultureInfoType))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(context.Compilation, includeObsoleteMethods: false, cultureInfoType);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, cultureInfoType.ToDisplayString());
                    return;
                }
            }
        }

        private static bool IsInvariantDateTimeFormat(IOperation valueOperation)
        {
            return valueOperation.ConstantValue.HasValue && valueOperation.ConstantValue.Value is "o" or "O" or "r" or "R" or "s" or "u";
        }
    }
}
