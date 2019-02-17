using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotUseStringGetHashCodeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotUseStringGetHashCode,
            title: "Use StringComparer.GetHashCode",
            messageFormat: "Use StringComparer.GetHashCode",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseStringGetHashCode));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (string.Equals(operation.TargetMethod.Name, nameof(string.GetHashCode), StringComparison.Ordinal) &&
                operation.TargetMethod.ContainingType.IsString())
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), operation.TargetMethod.Name));
            }
        }
    }
}
