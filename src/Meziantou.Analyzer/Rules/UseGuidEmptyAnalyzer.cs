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
            var guidType = compilationContext.Compilation.GetBestTypeByMetadataName("System.Guid");
            if (guidType is null)
                return;

            compilationContext.RegisterOperationAction(ctx => AnalyzeObjectCreationOperation(ctx, guidType), OperationKind.ObjectCreation);
            compilationContext.RegisterOperationAction(ctx => AnalyzeInvocationOperation(ctx, guidType), OperationKind.Invocation);
        });
    }

    private static void AnalyzeObjectCreationOperation(OperationAnalysisContext context, INamedTypeSymbol guidType)
    {
        var operation = (IObjectCreationOperation)context.Operation;

        if (operation.Constructor is null || !operation.Constructor.ContainingType.IsEqualTo(guidType))
            return;

        if (operation.Arguments.Length == 0)
        {
            context.ReportDiagnostic(Rule, operation);
            return;
        }

        if (operation.Arguments is [{ Value.Type.SpecialType: SpecialType.System_String, Value.ConstantValue: { HasValue: true, Value: string value } }])
        {
            if (System.Guid.TryParse(value, out var guid) && guid == System.Guid.Empty)
            {
                context.ReportDiagnostic(Rule, operation);
            }

            return;
        }

        if (operation.Arguments.Length == 11 && IsAllZero(operation.Arguments))
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static bool IsAllZero(ImmutableArray<IArgumentOperation> arguments)
    {
        foreach (var argument in arguments)
        {
            if (!argument.Value.ConstantValue.HasValue || !NumericHelpers.IsZero(argument.Value.ConstantValue.Value))
                return false;
        }

        return true;
    }

    private static void AnalyzeInvocationOperation(OperationAnalysisContext context, INamedTypeSymbol guidType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!invocation.TargetMethod.ContainingType.IsEqualTo(guidType))
            return;

        if (invocation is { TargetMethod.Name: "Parse", Arguments: [{ Value.Type.SpecialType: SpecialType.System_String, Value.ConstantValue: { HasValue: true, Value: string value } }] })
        {
            if (System.Guid.TryParse(value, out var guid) && guid == System.Guid.Empty)
            {
                context.ReportDiagnostic(Rule, invocation);
            }
        }
    }
}
