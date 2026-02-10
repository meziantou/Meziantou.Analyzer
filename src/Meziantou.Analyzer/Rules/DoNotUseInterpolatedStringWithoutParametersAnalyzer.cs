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
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
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
            var interpolatedStringHandlerAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute");

            ctx.RegisterOperationAction(context => AnalyzeInterpolatedString(context, formattableStringSymbol, interpolatedStringHandlerAttributeSymbol), OperationKind.InterpolatedString);
        });
    }

    private static void AnalyzeInterpolatedString(OperationAnalysisContext context, INamedTypeSymbol? formattableStringSymbol, INamedTypeSymbol? interpolatedStringHandlerAttributeSymbol)
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

        // Check if the operation itself is typed as a custom handler (for empty strings)
        if (IsInterpolatedStringHandler(operation.Type, interpolatedStringHandlerAttributeSymbol))
            return;

        // Walk up the parent chain to find the target type
        var current = operation.Parent;
        while (current is not null)
        {
            // Check for conversion to FormattableString or custom handler
            if (current is IConversionOperation conversionOperation)
            {
                // If converting to FormattableString, don't report
                if (conversionOperation.Type?.IsEqualTo(formattableStringSymbol) == true)
                    return;

                // If converting to a custom InterpolatedStringHandler, don't report
                if (IsInterpolatedStringHandler(conversionOperation.Type, interpolatedStringHandlerAttributeSymbol))
                    return;
            }

            // Check if used as method argument with custom InterpolatedStringHandler parameter
            if (current is IArgumentOperation argumentOperation)
            {
                if (argumentOperation.Parameter?.Type is not null)
                {
                    if (IsInterpolatedStringHandler(argumentOperation.Parameter.Type, interpolatedStringHandlerAttributeSymbol))
                        return;
                }
            }

            // If assigned to FormattableString or custom handler, don't report
            if (current is IVariableInitializerOperation variableInitializer)
            {
                if (variableInitializer.Parent is IVariableDeclaratorOperation declarator)
                {
                    if (declarator.Symbol?.Type?.IsEqualTo(formattableStringSymbol) == true)
                        return;

                    if (IsInterpolatedStringHandler(declarator.Symbol?.Type, interpolatedStringHandlerAttributeSymbol))
                        return;
                }
            }

            current = current.Parent;
        }

        // Report diagnostic as a suggestion (Hidden severity with unnecessary tag)
        var diagnostic = Diagnostic.Create(Rule, operation.Syntax.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsInterpolatedStringHandler(ITypeSymbol? typeSymbol, INamedTypeSymbol? interpolatedStringHandlerAttributeSymbol)
    {
        if (typeSymbol is null || interpolatedStringHandlerAttributeSymbol is null)
            return false;

        return typeSymbol.HasAttribute(interpolatedStringHandlerAttributeSymbol);
    }
}
