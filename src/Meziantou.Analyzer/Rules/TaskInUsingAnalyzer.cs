﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TaskInUsingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.TaskInUsing,
        title: "Await task in using statement",
        messageFormat: "Await task in using statement",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TaskInUsing));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var taskSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskSymbol is null)
                return;

            var analyzerContext = new AnalyzerContext(taskSymbol);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeUsing, OperationKind.Using);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeUsingDeclaration, OperationKind.UsingDeclaration);
        });
    }

    private sealed class AnalyzerContext(INamedTypeSymbol taskSymbol)
    {
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
            if (operation is null)
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
            if (operation.Type is not null && operation.Type.IsOrInheritFrom(taskSymbol))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }
}
