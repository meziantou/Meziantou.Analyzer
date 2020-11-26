using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseGuidEmptyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.UseGuidEmpty,
            title: "Use Guid.Empty",
            messageFormat: "Use Guid.Empty",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseGuidEmpty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterOperationAction(AnalyzeObjectCreationOperation, OperationKind.ObjectCreation);
            });
        }

        private static void AnalyzeObjectCreationOperation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            var guidType = context.Compilation.GetTypeByMetadataName("System.Guid");
            if (operation.Type.IsEqualTo(guidType) && operation.Arguments.Length == 0)
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
