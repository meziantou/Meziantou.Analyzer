using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeGuidCreationAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.OptimizeGuidCreation,
        title: "Optimize guid creation",
        messageFormat: "Optimize guid creation",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.OptimizeGuidCreation));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var type = ctx.Compilation.GetBestTypeByMetadataName("System.Guid");
            if (type is null)
                return;

            ctx.RegisterOperationAction(symbolContext => AnalyzeInvocation(symbolContext, type), OperationKind.Invocation); // Guid.Parse(""), Guid.TryParse("")
            ctx.RegisterOperationAction(symbolContext => AnalyzeObjectCreation(symbolContext, type), OperationKind.ObjectCreation); // new Guid("")
        });
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext symbolContext, INamedTypeSymbol type)
    {
        var creation = (IObjectCreationOperation)symbolContext.Operation;
        if (creation.Constructor is null)
            return;

        if (!creation.Constructor.ContainingType.IsEqualTo(type))
            return;

        if (creation is { Arguments: [{ Value.Type.SpecialType: SpecialType.System_String, Value.ConstantValue: { HasValue: true, Value: string value } }] })
        {
            if (Guid.TryParse(value, out _))
            {
                symbolContext.ReportDiagnostic(Rule, creation);
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol guidType)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!invocation.TargetMethod.ContainingType.IsEqualTo(guidType))
            return;

        if (invocation is { TargetMethod.Name: "Parse", Arguments: [{ Value.Type.SpecialType: SpecialType.System_String, Value.ConstantValue: { HasValue: true, Value: string value } }] })
        {
            if (Guid.TryParse(value, out _))
            {
                context.ReportDiagnostic(Rule, invocation);
            }
        }
    }
}
