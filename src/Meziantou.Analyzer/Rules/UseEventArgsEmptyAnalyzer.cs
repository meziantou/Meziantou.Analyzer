using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseEventArgsEmptyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseEventArgsEmpty,
        title: "Use EventArgs.Empty",
        messageFormat: "Use EventArgs.Empty instead of new EventArgs()",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseEventArgsEmpty));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var type = context.Compilation.GetBestTypeByMetadataName("System.EventArgs");
            if (type is null)
                return;

            context.RegisterOperationAction(context => Analyze(context, type), OperationKind.ObjectCreation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol type)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        if (operation is null || operation.Constructor is null)
            return;

        if (operation.Arguments.Length > 0)
            return;

        if (operation.Constructor.ContainingType.IsEqualTo(type))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }
}
