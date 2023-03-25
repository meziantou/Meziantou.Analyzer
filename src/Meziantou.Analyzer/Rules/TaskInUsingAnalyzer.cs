using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TaskInUsingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.TaskInUsing,
        title: "Await task in using statement",
        messageFormat: "Await task in using statement",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TaskInUsing));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var taskSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskSymbol == null)
                return;

            var analyzerContext = new AnalyzerContext(taskSymbol);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeUsing, OperationKind.Using);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeUsingDeclaration, OperationKind.UsingDeclaration);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly INamedTypeSymbol _taskSymbol;

        public AnalyzerContext(INamedTypeSymbol taskSymbol)
        {
            _taskSymbol = taskSymbol;
        }

        public void AnalyzeUsing(OperationAnalysisContext context)
        {
            var operation = (IUsingOperation)context.Operation;
            AnalyzeResource(context, operation.Resources);
        }

        internal void AnalyzeUsingDeclaration(OperationAnalysisContext context)
        {
            var operation = (IUsingDeclarationOperation)context.Operation;
            AnalyzeResource(context, operation.DeclarationGroup);
        }

        private void AnalyzeResource(OperationAnalysisContext context, IOperation? operation)
        {
            if (operation == null)
                return;

            if (operation is IVariableDeclarationGroupOperation variableDeclarationGroupOperation)
            {
                foreach (var declaration in variableDeclarationGroupOperation.Declarations)
                {
                    AnalyzeResource(context, declaration);
                }

                return;
            }

            if (operation is IVariableDeclarationOperation variableDeclarationOperation)
            {
                foreach (var declarator in variableDeclarationOperation.Declarators)
                {
                    AnalyzeResource(context, declarator.Initializer?.Value);
                }
                return;
            }

            operation = operation.UnwrapImplicitConversionOperations();
            if (operation.Type != null && operation.Type.IsOrInheritFrom(_taskSymbol))
            {
                context.ReportDiagnostic(s_rule, operation);
            }
        }
    }
}
