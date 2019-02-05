using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseRegexTimeoutAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string[] s_methodNames = { "IsMatch", "Match", "Matches", "Replace", "Split" };

        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.MissingTimeoutParameterForRegex,
            title: "Add timeout parameter",
            messageFormat: "Add timeout parameter",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingStructLayoutAttribute));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (op == null || op.TargetMethod == null)
                return;

            if (!op.TargetMethod.IsStatic)
                return;

            var regexType = context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex");
            if (regexType == null)
                return;

            if (!s_methodNames.Contains(op.TargetMethod.Name, StringComparer.Ordinal))
                return;

            if (!regexType.Equals(op.TargetMethod.ContainingSymbol))
                return;

            // TODO contains TimeSpan parameter?
        }

        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            // TODO
        }
    }
}
