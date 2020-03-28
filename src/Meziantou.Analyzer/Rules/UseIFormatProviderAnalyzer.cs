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
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
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

                // Guid.ToString(IFormatProvider) should not be used
                if (operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.Guid")))
                    return;
            }

            if (!operation.HasArgumentOfType(formatProviderType))
            {
                if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(context.Compilation, formatProviderType) ||
                    (operation.TargetMethod.ContainingType.IsNumberType() && operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(context.Compilation, formatProviderType, numberStyleType)) ||
                    (operation.TargetMethod.ContainingType.IsDateTime() && operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(context.Compilation, formatProviderType, dateTimeStyleType)))
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, formatProviderType.ToDisplayString());
                    return;
                }
            }

            if (!operation.HasArgumentOfType(cultureInfoType))
            {
                if (operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(context.Compilation, includeObsoleteMethods: false, cultureInfoType) != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, cultureInfoType.ToDisplayString());
                    return;
                }
            }
        }
    }
}
