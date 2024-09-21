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
    private static readonly DiagnosticDescriptor AsyncSuffixRule = new(
        RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix,
        title: "Use 'Async' suffix when a method returns an awaitable type",
        messageFormat: "Method returning an awaitable type must use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsReturningAnAwaitableTypeMustHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor NotAsyncSuffixRule = new(
        RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix,
        title: "Do not use 'Async' suffix when a method does not return an awaitable type",
        messageFormat: "Method not returning an awaitable type must not use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsNotReturningAnAwaitableTypeMustNotHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor AsyncSuffixRuleAsyncEnumerable = new(
       RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix,
       title: "Use 'Async' suffix when a method returns IAsyncEnumerable<T>",
       messageFormat: "Method returning IAsyncEnumerable<T> must use the 'Async' suffix",
       RuleCategories.Design,
       DiagnosticSeverity.Warning,
       isEnabledByDefault: false,
       description: "",
       helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsReturningIAsyncEnumerableMustHaveTheAsyncSuffix));

    private static readonly DiagnosticDescriptor NotAsyncSuffixRuleAsyncEnumerable = new(
        RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix,
        title: "Do not use 'Async' suffix when a method does not return IAsyncEnumerable<T>",
        messageFormat: "Method not returning IAsyncEnumerable<T> must not use the 'Async' suffix",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MethodsNotReturningIAsyncEnumerableMustNotHaveTheAsyncSuffix));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(AsyncSuffixRule, NotAsyncSuffixRule, AsyncSuffixRuleAsyncEnumerable, NotAsyncSuffixRuleAsyncEnumerable);

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

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly AwaitableTypes _awaitableTypes = new(compilation);
        private readonly INamedTypeSymbol? _iasyncEnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        private readonly INamedTypeSymbol? _benchmarkSymbol = compilation.GetBestTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

        public void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.IsOverrideOrInterfaceImplementation())
                return;

            if (method.IsTopLevelStatementsEntryPointMethod())
                return;

            if (method.IsEqualTo(context.Compilation.GetEntryPoint(context.CancellationToken)))
                return;

            if (MustIgnoreSymbol(method))
                return;

            var hasAsyncSuffix = method.Name.EndsWith("Async", StringComparison.Ordinal);
            if (_awaitableTypes.IsAwaitable(method.ReturnType, context.Compilation))
            {
                if (!hasAsyncSuffix)
                {
                    context.ReportDiagnostic(AsyncSuffixRule, method);
                }
            }
            else if ((method.ReturnType as INamedTypeSymbol)?.ConstructedFrom.IsOrImplements(_iasyncEnumerableSymbol) is true)
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRuleAsyncEnumerable, method);
                }
                else
                {
                    context.ReportDiagnostic(AsyncSuffixRuleAsyncEnumerable, method);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRule, method);
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
                    context.ReportDiagnostic(AsyncSuffixRule, properties: default, operation, DiagnosticMethodReportOptions.ReportOnMethodName);
                }
            }
            else if ((method.ReturnType as INamedTypeSymbol)?.ConstructedFrom.IsOrImplements(_iasyncEnumerableSymbol) is true)
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRuleAsyncEnumerable, method);
                }
                else
                {
                    context.ReportDiagnostic(AsyncSuffixRuleAsyncEnumerable, method);
                }
            }
            else
            {
                if (hasAsyncSuffix)
                {
                    context.ReportDiagnostic(NotAsyncSuffixRule, properties: default, operation, DiagnosticMethodReportOptions.ReportOnMethodName);
                }
            }
        }

        private bool MustIgnoreSymbol(IMethodSymbol symbol)
        {
            if (symbol.HasAttribute(_benchmarkSymbol))
                return true;

            if (symbol.IsUnitTestMethod())
                return true;

            return false;
        }
    }
}
