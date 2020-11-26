using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotRaiseNotImplementedExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new(
            RuleIdentifiers.DoNotRaiseNotImplementedException,
            title: "Implement the functionality instead of throwing NotImplementedException",
            messageFormat: "Implement the functionality (or raise NotSupportedException or PlatformNotSupportedException)",
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

        private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol reservedExceptionType)
        {
            var operation = (IThrowOperation)context.Operation;
            if (operation == null || operation.Exception == null)
                return;

            var exceptionType = operation.Exception.GetActualType();
            if (exceptionType.IsEqualTo(reservedExceptionType))
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
