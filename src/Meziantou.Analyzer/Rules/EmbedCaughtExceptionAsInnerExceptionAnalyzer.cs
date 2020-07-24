using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EmbedCaughtExceptionAsInnerExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.EmbedCaughtExceptionAsInnerException,
            title: "Embed the caught exception as innerException",
            messageFormat: "Embed the caught exception as innerException",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterOperationAction(AnalyzeThrow, OperationKind.Throw);
        }

        private static void AnalyzeThrow(OperationAnalysisContext context)
        {
            var operation = (IThrowOperation)context.Operation;
            if (operation.Exception == null)
                return;

            var compilation = context.Compilation;
            var exceptionSymbol = compilation.GetTypeByMetadataName("System.Exception");
            if (exceptionSymbol == null)
                return;

            var catchOperation = operation.Ancestors().OfType<ICatchClauseOperation>().FirstOrDefault();
            if (catchOperation == null)
                return;

            if (operation.Exception is IObjectCreationOperation objectCreationOperation)
            {
                var argument = objectCreationOperation.Arguments.FirstOrDefault(arg => IsPotentialParameter(arg.Parameter, exceptionSymbol));
                if (argument == null)
                {
                    if (objectCreationOperation.Constructor.HasOverloadWithAdditionalParameterOfType(context.Compilation, exceptionSymbol))
                    {
                        context.ReportDiagnostic(s_rule, objectCreationOperation);
                    }
                }
            }
        }

        private static bool IsPotentialParameter(IParameterSymbol parameter, ITypeSymbol exceptionSymbol)
        {
            return parameter.Type.IsOrInheritFrom(exceptionSymbol);
        }
    }
}
