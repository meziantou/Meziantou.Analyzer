using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringCreateInsteadOfFormattableStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStringCreateInsteadOfFormattableString,
        title: "Use string.Create instead of FormattableString",
        messageFormat: "Use string.Create instead of FormattableString",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringCreateInsteadOfFormattableString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            if (!ctx.Compilation.GetCSharpLanguageVersion().IsCSharp10OrAbove())
                return;

            var formatProviderSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IFormatProvider");
            var defaultInterpolatedStringHandlerSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler");

            var stringCreateSymbol = ctx.Compilation.GetSpecialType(SpecialType.System_String)
                .GetMembers("Create")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.ReturnType.IsString() && m.Parameters.Length == 2 && m.Parameters[0].Type.IsEqualTo(formatProviderSymbol) && m.Parameters[1].Type.IsEqualTo(defaultInterpolatedStringHandlerSymbol));

            var formattableStringSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.FormattableString");
            if (stringCreateSymbol is null || formatProviderSymbol is null)
                return;

            ctx.RegisterOperationAction(AnalyzeSymbol, OperationKind.Invocation);

            void AnalyzeSymbol(OperationAnalysisContext context)
            {
                var operation = (IInvocationOperation)context.Operation;
                var method = operation.TargetMethod;

                if (!method.ContainingType.IsEqualTo(formattableStringSymbol))
                    return;

                if (method.Name is "Invariant" or "CurrentCulture" && method.Parameters.Length == 1 && operation.Arguments[0].Value.UnwrapImplicitConversionOperations() is IInterpolatedStringOperation)
                {
                    context.ReportDiagnostic(Rule, operation);
                    return;
                }
            }
        });
    }
}
