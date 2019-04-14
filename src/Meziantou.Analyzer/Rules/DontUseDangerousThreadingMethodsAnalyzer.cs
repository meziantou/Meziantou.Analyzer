using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DontUseDangerousThreadingMethodsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DontUseDangerousThreadingMethods,
            title: "Don't use dangerous threading methods",
            messageFormat: "Don't use dangerous threading methods",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DontUseDangerousThreadingMethods));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(Analyze, OperationKind.Invocation);
        }

        private static void Analyze(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (string.Equals(op.TargetMethod.Name, "Abort", StringComparison.Ordinal) ||
                string.Equals(op.TargetMethod.Name, "Suspend", StringComparison.Ordinal) ||
                string.Equals(op.TargetMethod.Name, "Resume", StringComparison.Ordinal))
            {
                var types = context.Compilation.GetTypesByMetadataName("System.Threading.Thread");
                foreach (var type in types)
                {
                    if (op.TargetMethod.ContainingType.IsEqualTo(type))
                    {
                        context.ReportDiagnostic(s_rule, op);
                    }
                }
            }
        }
    }
}
