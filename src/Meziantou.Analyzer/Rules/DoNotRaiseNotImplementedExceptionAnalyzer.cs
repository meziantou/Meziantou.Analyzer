using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRaiseNotImplementedExceptionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotRaiseNotImplementedException,
        title: "Implement the functionality instead of throwing NotImplementedException",
        messageFormat: "Implement the functionality (or raise NotSupportedException or PlatformNotSupportedException)",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseNotImplementedException));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var compilation = ctx.Compilation;
            var type = compilation.GetBestTypeByMetadataName("System.NotImplementedException");

            if (type is not null)
            {
                ctx.RegisterOperationAction(_ => Analyze(_, type), OperationKind.Throw);
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
