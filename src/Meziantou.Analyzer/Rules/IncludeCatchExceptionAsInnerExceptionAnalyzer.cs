using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class IncludeCatchExceptionAsInnerExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.IncludeCatchExceptionAsInnerException,
            title: "Preserve the catched exception in the innerException",
            messageFormat: "Preserve the catched exception in the innerException",
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

                    return;
                }

                if (argument.Value is ILocalReferenceOperation argumentValue && catchOperation.Locals.Contains(argumentValue.Local))
                    return;

                context.ReportDiagnostic(s_rule, objectCreationOperation);
            }
        }

        private static bool IsPotentialParameter(IParameterSymbol parameter, ITypeSymbol exceptionSymbol)
        {
            return parameter.Type.IsAssignableTo(exceptionSymbol);
        }
    }
}
