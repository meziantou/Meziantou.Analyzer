using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotRaiseNotImplementedExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotRaiseNotImplementedException,
            title: "Do not raise System.NotImplementedException",
            messageFormat: "Do not raise System.NotImplementedException",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseNotImplementedException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var compilation = ctx.Compilation;
                var type = compilation.GetTypeByMetadataName("System.NotImplementedException");

                if (type != null)
                {
                    ctx.RegisterOperationAction(_ => Analyze(_, type), OperationKind.Throw);
                }
            });
        }

        private void Analyze(OperationAnalysisContext context, INamedTypeSymbol reservedExceptionType)
        {
            var operation = (IThrowOperation)context.Operation;
            if (operation == null || operation.Exception == null)
                return;

            var exceptionType = operation.Exception.Type;
            if (exceptionType.IsEqualsTo(reservedExceptionType))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, operation.Syntax.GetLocation()));
            }
        }
    }
}
