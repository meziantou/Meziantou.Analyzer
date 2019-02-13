using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseIFormatProviderParameter,
            title: "IFormatProvider is missing",
            messageFormat: "Use an overload of '{0}' that has an IFormatProvider parameter",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var formatProviderType = context.Compilation.GetTypeByMetadataName("System.IFormatProvider");
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
            }
            else if (!string.Equals(methodName, "Parse", StringComparison.Ordinal) &&
                     !string.Equals(methodName, "TryParse", StringComparison.Ordinal) &&
                     !string.Equals(methodName, "TryFormat", StringComparison.Ordinal))
            {
                return;
            }

            if (!UseStringComparisonAnalyzer.HasArgumentOfType(operation, formatProviderType))
            {
                if (UseStringComparisonAnalyzer.HasOverloadWithAdditionalParameterOfType(operation, formatProviderType) ||
                    (operation.TargetMethod.ContainingType.IsNumberType() && UseStringComparisonAnalyzer.HasOverloadWithAdditionalParameterOfType(operation, formatProviderType, numberStyleType)) ||
                    (operation.TargetMethod.ContainingType.IsDateTime() && UseStringComparisonAnalyzer.HasOverloadWithAdditionalParameterOfType(operation, formatProviderType, dateTimeStyleType)))
                {
                    var diagnostic = Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), operation.TargetMethod.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
