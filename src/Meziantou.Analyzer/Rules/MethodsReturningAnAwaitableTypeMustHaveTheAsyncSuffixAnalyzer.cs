using System;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffixAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_asyncSuffixRule = new(
        RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix,
        title: "Use 'Async' suffix when a method returns an awaitable type",
        messageFormat: "Method returning an awaitable type must use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor s_notAsyncSuffixRule = new(
        RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix,
        title: "Do not use 'Async' suffix when a method does not return an awaitable type",
        messageFormat: "Method not returning an awaitable type must not use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_asyncSuffixRule, s_notAsyncSuffixRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(ctx =>
        {
            var context = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterSymbolAction(context.AnalyzeSymbol, SymbolKind.Method);
            ctx.RegisterOperationAction(context.AnalyzeLocalFunction, OperationKind.LocalFunction);
        });
    }

    private sealed class AnalyzerContext
    {
        private readonly AwaitableTypes _awaitableTypes;

        public AnalyzerContext(Compilation compilation)
        {
            _awaitableTypes = new AwaitableTypes(compilation);
        }

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.IsOverrideOrInterfaceImplementation())
                return;

            var hasAsyncSuffix = method.Name.EndsWith("Async", StringComparison.Ordinal);
            if (_awaitableTypes.IsAwaitable(method.ReturnType, context.Compilation))
            {
                if (!hasAsyncSuffix)
                {
                    context.ReportDiagnostic(s_asyncSuffixRule, method);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(s_notAsyncSuffixRule, method);
                }
            }
        }

        public void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var operation = (ILocalFunctionOperation)context.Operation;
            var method = operation.Symbol;

            var hasAsyncSuffix = method.Name.EndsWith("Async", StringComparison.Ordinal);
            if (_awaitableTypes.IsAwaitable(method.ReturnType, context.Compilation))
            {
                if (!hasAsyncSuffix)
                {
                    context.ReportDiagnostic(s_asyncSuffixRule, properties: default, operation, DiagnosticReportOptions.ReportOnMethodName);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(s_notAsyncSuffixRule, properties: default, operation, DiagnosticReportOptions.ReportOnMethodName);
                }
            }
        }
    }
}
