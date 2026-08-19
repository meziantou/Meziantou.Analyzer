using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant,
        title: "Simplify string.Create when all parameters are culture invariant",
        messageFormat: "Simplify string.Create when all parameters are culture invariant",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant));

    private static readonly ConfigurationDefinition<bool> TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant + ".treat_opaque_runtime_types_as_culture_sensitive", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> TreatUnsealedTypesAsCultureSensitiveConfiguration = new(RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant + ".treat_unsealed_types_as_culture_sensitive", defaultValue: false);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            if (!ctx.Compilation.GetCSharpLanguageVersion().IsCSharp10OrGreater())
                return;

            var formatProviderSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.IFormatProvider");
            var cultureInfoSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
            var defaultInterpolatedStringHandlerSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler");

            var stringCreateSymbol = ctx.Compilation.GetSpecialType(SpecialType.System_String)
                .GetMembers("Create")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.ReturnType.IsString() && m.Parameters.Length == 2 && m.Parameters[0].Type.IsEqualTo(formatProviderSymbol) && m.Parameters[1].Type.IsEqualTo(defaultInterpolatedStringHandlerSymbol));

            if (stringCreateSymbol is null || cultureInfoSymbol is null)
                return;

            var cultureInfoInvariantCultureProperty = cultureInfoSymbol.GetMembers("InvariantCulture").OfType<IPropertySymbol>().FirstOrDefault();
            if (cultureInfoInvariantCultureProperty is null)
                return;

            var cultureSensitiveContext = new CultureSensitiveFormattingContext(ctx.Compilation);

            ctx.RegisterOperationAction(context => AnalyzeInvocation(context, stringCreateSymbol, cultureInfoInvariantCultureProperty, cultureSensitiveContext), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, IMethodSymbol stringCreateSymbol, IPropertySymbol cultureInfoInvariantCultureProperty, CultureSensitiveFormattingContext cultureSensitiveContext)
    {
        var operation = (IInvocationOperation)context.Operation;

        if (!operation.TargetMethod.IsEqualTo(stringCreateSymbol))
            return;

        // Check if the first argument is CultureInfo.InvariantCulture
        if (operation.Arguments.Length != 2)
            return;

        var cultureArgument = operation.Arguments[0].Value;
        if (!IsCultureInfoInvariantCulture(cultureArgument, cultureInfoInvariantCultureProperty))
            return;

        // Check if the second argument (interpolated string handler) has only culture-invariant parameters
        var interpolatedStringArgument = operation.Arguments[1].Value;

        if (interpolatedStringArgument is IInterpolatedStringHandlerCreationOperation handlerCreation)
        {
            var interpolatedStringContent = handlerCreation.Content;
            // Use UnwrapNullableOfT to check the underlying type of nullable types
            var options = GetOptions(context);
            if (CultureSensitiveFormattingContext.IsCultureInsensitive(cultureSensitiveContext.GetCultureSensitivity(interpolatedStringContent, options), options))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }

    private static CultureSensitiveOptions GetOptions(OperationAnalysisContext context)
    {
        var options = CultureSensitiveOptions.UnwrapNullableOfT;

        if (context.Options.GetConfigurationValue(context.Operation.Syntax.SyntaxTree, TreatOpaqueRuntimeTypesAsCultureSensitiveConfiguration))
            options |= CultureSensitiveOptions.TreatOpaqueRuntimeTypesAsCultureSensitive;

        if (context.Options.GetConfigurationValue(context.Operation.Syntax.SyntaxTree, TreatUnsealedTypesAsCultureSensitiveConfiguration))
            options |= CultureSensitiveOptions.TreatUnsealedTypesAsCultureSensitive;

        return options;
    }

    private static bool IsCultureInfoInvariantCulture(IOperation operation, IPropertySymbol cultureInfoInvariantCultureProperty)
    {
        operation = operation.UnwrapImplicitConversions();

        if (operation is IPropertyReferenceOperation propertyReference)
        {
            return SymbolEqualityComparer.Default.Equals(propertyReference.Property, cultureInfoInvariantCultureProperty);
        }

        return false;
    }
}
