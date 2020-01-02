using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ValueReturnedByStreamReadShouldBeUsedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.TheReturnValueOfStreamReadShouldBeUsed,
            title: "The value returned by Stream.Read is not used",
            messageFormat: "The value returned by Stream.Read is not used",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TheReturnValueOfStreamReadShouldBeUsed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod;
            if (targetMethod.Name != nameof(Stream.Read) && targetMethod.Name != nameof(Stream.ReadAsync))
                return;

            var streamSymbol = context.Compilation.GetTypeByMetadataName("System.IO.Stream");
            if (!targetMethod.ContainingType.IsOrInheritFrom(streamSymbol))
                return;

            var parent = invocation.Parent;
            if (parent is IAwaitOperation)
            {
                parent = parent.Parent;
            }

            if (parent == null || parent is IBlockOperation || parent is IExpressionStatementOperation)
            {
                context.ReportDiagnostic(s_rule, invocation);
            }
        }
    }
}
