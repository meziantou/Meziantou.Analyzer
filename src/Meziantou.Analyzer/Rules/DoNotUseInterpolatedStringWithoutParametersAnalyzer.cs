using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseInterpolatedStringWithoutParametersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseInterpolatedStringWithoutParameters,
        title: "Do not use interpolated string without parameters",
        messageFormat: "Do not use interpolated string without parameters",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseInterpolatedStringWithoutParameters));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var formattableStringSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.FormattableString");

            ctx.RegisterOperationAction(context => AnalyzeInterpolatedString(context, formattableStringSymbol), OperationKind.InterpolatedString);
        });
    }

    private static void AnalyzeInterpolatedString(OperationAnalysisContext context, INamedTypeSymbol? formattableStringSymbol)
    {
        var operation = (IInterpolatedStringOperation)context.Operation;

        // Only report if there are no interpolations (no parameters)
        if (operation.Parts.Any(part => part is IInterpolationOperation))
            return;

#if CSHARP10_OR_GREATER
        // If there are IInterpolatedStringAppendOperation parts, it means a custom handler is being used
        if (operation.Parts.Any(part => part is IInterpolatedStringAppendOperation))
            return;
#endif

        // Check if the target type is FormattableString
        var parent = operation.Parent;
        if (parent is IConversionOperation conversionOperation)
        {
            // If converting to FormattableString, don't report
            if (conversionOperation.Type?.IsEqualTo(formattableStringSymbol) == true)
                return;

            // If converting to a custom InterpolatedStringHandler, don't report
            if (conversionOperation.Type is INamedTypeSymbol namedType)
            {
                if (namedType.Name.EndsWith("InterpolatedStringHandler", StringComparison.Ordinal))
                    return;
            }
        }

        // If assigned to FormattableString, don't report
        if (parent is IVariableInitializerOperation variableInitializer)
        {
            if (variableInitializer.Parent is IVariableDeclaratorOperation declarator)
            {
                if (declarator.Symbol?.Type?.IsEqualTo(formattableStringSymbol) == true)
                    return;

                if (declarator.Symbol?.Type is INamedTypeSymbol namedType)
                {
                    if (namedType.Name.EndsWith("InterpolatedStringHandler", StringComparison.Ordinal))
                        return;
                }
            }
        }

        // Report diagnostic as a suggestion (Hidden severity with unnecessary tag)
        var diagnostic = Diagnostic.Create(Rule, operation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
