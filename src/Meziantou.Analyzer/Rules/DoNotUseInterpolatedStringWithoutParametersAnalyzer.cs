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
        title: "Do not use interpolated string without parameters as argument for interpolated string handler",
        messageFormat: "Do not use interpolated string without parameters as argument for interpolated string handler",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
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
            var interpolatedStringHandlerAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute");
            if (interpolatedStringHandlerAttributeSymbol is null)
                return;

            ctx.RegisterOperationAction(context => AnalyzeArgument(context, interpolatedStringHandlerAttributeSymbol), OperationKind.Argument);
        });
    }

    private static void AnalyzeArgument(OperationAnalysisContext context, INamedTypeSymbol interpolatedStringHandlerAttributeSymbol)
    {
        var argument = (IArgumentOperation)context.Operation;

        // Check if the parameter type is an interpolated string handler
        if (argument.Parameter is null)
            return;

        var parameterType = argument.Parameter.Type;
        if (!parameterType.HasAttribute(interpolatedStringHandlerAttributeSymbol))
            return;

        // Get the actual value being passed
        var value = argument.Value.UnwrapImplicitConversionOperations();

        // Check if it's an interpolated string handler creation operation
        if (value is not IInterpolatedStringHandlerCreationOperation handlerCreation)
            return;

        // Get the interpolated string content
        if (handlerCreation.Content is not IInterpolatedStringOperation interpolatedString)
            return;

        // Check if the interpolated string has any interpolations
        if (HasInterpolations(interpolatedString))
            return;

        // Report diagnostic - interpolated string without parameters
        context.ReportDiagnostic(Rule, interpolatedString);
    }

    private static bool HasInterpolations(IInterpolatedStringOperation interpolatedString)
    {
        foreach (var part in interpolatedString.Parts)
        {
            // Pre-C# 10 or when not using string handlers
            if (part is IInterpolationOperation)
                return true;

#if CSHARP10_OR_GREATER
            // C# 10+ with string handlers
            // IInterpolatedStringTextOperation is for literal text
            // IInterpolatedStringAppendOperation could be for text or interpolations
            // We need to differentiate between them
            if (part is IInterpolatedStringAppendOperation appendOp)
            {
                // Check if this append is for an interpolation (has value to format)
                // Text appends typically have different signatures
                if (appendOp.AppendCall is IInvocationOperation invocation)
                {
                    var methodName = invocation.TargetMethod.Name;
                    // AppendLiteral is for literal text, AppendFormatted is for interpolations
                    if (methodName == "AppendFormatted")
                        return true;
                }
            }
#endif
        }

        return false;
    }
}
