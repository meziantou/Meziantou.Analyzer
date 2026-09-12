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
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var formattableStringSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.FormattableString");
            var formattableSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IFormattable");
            var interpolatedStringHandlerAttributeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute");

            ctx.RegisterOperationAction(context => AnalyzeInterpolatedString(context, formattableStringSymbol, formattableSymbol, interpolatedStringHandlerAttributeSymbol), OperationKind.InterpolatedString);
        });
    }

    private static void AnalyzeInterpolatedString(OperationAnalysisContext context, INamedTypeSymbol? formattableStringSymbol, INamedTypeSymbol? formattableSymbol, INamedTypeSymbol? interpolatedStringHandlerAttributeSymbol)
    {
        var operation = (IInterpolatedStringOperation)context.Operation;

        // Only report if there are no interpolations (no parameters)
        if (operation.Parts.Any(part => part is IInterpolationOperation))
            return;

        // If there are IInterpolatedStringAppendOperation parts, it means a custom handler is being used
        if (operation.Parts.Any(part => part is IInterpolatedStringAppendOperation))
            return;

        // Check if the operation itself is typed as a custom handler (for empty strings)
        if (IsInterpolatedStringHandler(operation.Type, interpolatedStringHandlerAttributeSymbol))
            return;

        // An interpolated string can be converted to FormattableString, IFormattable, or a custom handler, but a
        // string literal cannot. Only the conversion applied to the interpolated string itself matters, as the
        // conversions of the enclosing operations still apply to the string literal.
        if (operation.Parent is IInterpolatedStringHandlerCreationOperation)
            return;

        if (operation.Parent is IConversionOperation conversionOperation)
        {
            var targetType = conversionOperation.Type;
            if (targetType.IsEqualTo(formattableStringSymbol) || targetType.IsEqualTo(formattableSymbol))
                return;
        }

        // Report diagnostic as a suggestion (Hidden severity with unnecessary tag)
        context.ReportDiagnostic(Rule, operation);
    }

    private static bool IsInterpolatedStringHandler(ITypeSymbol? typeSymbol, INamedTypeSymbol? interpolatedStringHandlerAttributeSymbol)
    {
        if (typeSymbol is null || interpolatedStringHandlerAttributeSymbol is null)
            return false;

        return typeSymbol.HasAttribute(interpolatedStringHandlerAttributeSymbol);
    }
}
