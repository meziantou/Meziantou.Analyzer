using System.Collections.Immutable;
using System.IO;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueReturnedByStreamReadShouldBeUsedAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.TheReturnValueOfStreamReadShouldBeUsed,
        title: "The value returned by Stream.Read/Stream.ReadAsync is not used",
        messageFormat: "The value returned by '{0}' is not used",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TheReturnValueOfStreamReadShouldBeUsed));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var streamSymbol = context.Compilation.GetBestTypeByMetadataName("System.IO.Stream");
            if (streamSymbol is null)
                return;

            context.RegisterOperationAction(context => AnalyzeOperation(context, streamSymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol streamSymbol)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var targetMethod = invocation.TargetMethod;
        if (targetMethod.Name != nameof(Stream.Read) && targetMethod.Name != nameof(Stream.ReadAsync))
            return;

        if (!targetMethod.ContainingType.IsOrInheritFrom(streamSymbol))
            return;

        var parent = invocation.Parent;
        if (parent is IAwaitOperation)
        {
            parent = parent.Parent;
        }

        if (parent is null || parent is IBlockOperation || parent is IExpressionStatementOperation)
        {
            context.ReportDiagnostic(Rule, invocation, targetMethod.Name);
        }
    }
}
