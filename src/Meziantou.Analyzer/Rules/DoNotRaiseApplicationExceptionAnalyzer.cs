using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotRaiseApplicationExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.DoNotRaiseApplicationException,
            title: "Do not raise System.ApplicationException type",
            messageFormat: "Do not raise System.ApplicationException type",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseApplicationException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(ctx =>
            {
                var reservedExceptionType = ctx.Compilation.GetTypeByMetadataName("System.ApplicationException");
                if (reservedExceptionType != null)
                {
                    ctx.RegisterOperationAction(_ => Analyze(_, reservedExceptionType), OperationKind.Throw);
                }
            });
        }

        private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol reservedExceptionType)
        {
            var operation = (IThrowOperation)context.Operation;
            if (operation == null || operation.Exception == null)
                return;

            var exceptionType = operation.Exception.Type;
            if (exceptionType.IsEqualTo(reservedExceptionType))
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
