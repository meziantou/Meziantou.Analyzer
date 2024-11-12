using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseGuidEmptyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseGuidEmpty,
        title: "Use Guid.Empty",
        messageFormat: "Use Guid.Empty",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseGuidEmpty));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
        var guidType = context.Compilation.GetBestTypeByMetadataName("System.Guid");
        if (operation.Type.IsEqualTo(guidType) && operation.Arguments.Length == 0)
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}
