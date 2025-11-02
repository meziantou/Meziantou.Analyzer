using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EmbedCaughtExceptionAsInnerExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.EmbedCaughtExceptionAsInnerException,
        title: "Embed the caught exception as innerException",
        messageFormat: "Embed the caught exception as innerException",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EmbedCaughtExceptionAsInnerException));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var overloadFinder = new OverloadFinder(context.Compilation);
            var exceptionSymbol = context.Compilation.GetBestTypeByMetadataName("System.Exception");
            if (exceptionSymbol is null)
                return;

            context.RegisterOperationAction(context => AnalyzeThrow(context, overloadFinder, exceptionSymbol), OperationKind.Throw);
        });
    }

    private static void AnalyzeThrow(OperationAnalysisContext context, OverloadFinder overloadFinder, INamedTypeSymbol exceptionSymbol)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation.Exception is null)
            return;

        var catchOperation = operation.Ancestors().OfType<ICatchClauseOperation>().FirstOrDefault();
        if (catchOperation is null)
            return;

        if (operation.Exception is IObjectCreationOperation objectCreationOperation)
        {
            if (objectCreationOperation.Constructor is null)
                return;

            var argument = objectCreationOperation.Arguments.FirstOrDefault(arg => IsPotentialParameter(arg?.Parameter, exceptionSymbol));
            if (argument is null)
            {
                if (overloadFinder.HasOverloadWithAdditionalParameterOfType(objectCreationOperation, options: default, [exceptionSymbol]))
                {
                    context.ReportDiagnostic(Rule, objectCreationOperation);
                }
            }
        }
    }

    private static bool IsPotentialParameter(IParameterSymbol? parameter, ITypeSymbol exceptionSymbol)
    {
        if (parameter is null)
            return false;

        return parameter.Type.IsOrInheritFrom(exceptionSymbol);
    }
}
