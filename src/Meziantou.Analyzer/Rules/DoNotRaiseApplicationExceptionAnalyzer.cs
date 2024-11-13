using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRaiseApplicationExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotRaiseApplicationException,
        title: "Do not raise System.ApplicationException type",
        messageFormat: "Do not raise System.ApplicationException type",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseApplicationException));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var reservedExceptionType = ctx.Compilation.GetBestTypeByMetadataName("System.ApplicationException");
            if (reservedExceptionType is not null)
            {
                ctx.RegisterOperationAction(_ => Analyze(_, reservedExceptionType), OperationKind.Throw);
            }
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol reservedExceptionType)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation is null || operation.Exception is null)
            return;

        var exceptionType = operation.Exception.GetActualType();
        if (exceptionType.IsEqualTo(reservedExceptionType))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}
