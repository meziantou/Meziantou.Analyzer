using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringCreateWithCommonFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseStringCreateWithCommonFormatProvider,
        title: "Use String.Create with IFormatProvider when all interpolated string parameters use the same format provider",
        messageFormat: "Use String.Create with IFormatProvider when all interpolated string parameters use the same format provider",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Improves performance by using String.Create with a common IFormatProvider instead of multiple ToString calls with the same format provider.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringCreateWithCommonFormatProvider));

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
            if (formatProviderSymbol is null)
                return;

            ctx.RegisterOperationAction(context => AnalyzeInterpolatedString(context, formatProviderSymbol), OperationKind.InterpolatedString);
        });
    }

    private static void AnalyzeInterpolatedString(OperationAnalysisContext context, INamedTypeSymbol formatProviderSymbol)
    {
        var operation = (IInterpolatedStringOperation)context.Operation;

        // Skip if the interpolated string is a constant (no interpolations)
        if (operation.ConstantValue.HasValue)
            return;

        // Skip if used in FormattableString context (already explicitly culture-aware)
        if (operation.Parent is IConversionOperation conversionOperation)
        {
            var formattableStringSymbol = context.Compilation.GetBestTypeByMetadataName("System.FormattableString");
            if (conversionOperation.Type.IsEqualTo(formattableStringSymbol))
                return;
        }

        // Skip if already used with String.Create
        if (operation.Parent?.Parent is IArgumentOperation argumentOp && argumentOp.Parent is IInvocationOperation invocationOp)
        {
            if (invocationOp.TargetMethod.Name == "Create" && invocationOp.TargetMethod.ContainingType.IsString())
                return;
        }

        // Collect all interpolation parts that are ToString calls with IFormatProvider
        var toStringCallsWithFormatProvider = new List<(IInterpolationOperation Interpolation, IInvocationOperation ToStringCall, IOperation FormatProvider)>();

        foreach (var part in operation.Parts)
        {
            if (part is not IInterpolationOperation interpolation)
                continue;

            var expression = interpolation.Expression.UnwrapImplicitConversionOperations();

            // Check if the expression is a ToString call
            if (expression is IInvocationOperation toStringInvocation &&
                toStringInvocation.TargetMethod.Name == "ToString" &&
                toStringInvocation.TargetMethod.Parameters.Length > 0)
            {
                // Find IFormatProvider parameter
                IOperation? formatProviderArg = null;
                foreach (var arg in toStringInvocation.Arguments)
                {
                    if (arg.Parameter?.Type.IsOrInheritFrom(formatProviderSymbol) == true)
                    {
                        formatProviderArg = arg.Value;
                        break;
                    }
                }

                if (formatProviderArg is not null)
                {
                    toStringCallsWithFormatProvider.Add((interpolation, toStringInvocation, formatProviderArg));
                }
            }
        }

        // Need at least 2 ToString calls with format providers to make the optimization worthwhile
        if (toStringCallsWithFormatProvider.Count < 2)
            return;

        // Check if all format providers are semantically equivalent
        var firstFormatProvider = toStringCallsWithFormatProvider[0].FormatProvider;
        var allSame = true;

        for (int i = 1; i < toStringCallsWithFormatProvider.Count; i++)
        {
            if (!AreEquivalentFormatProviders(firstFormatProvider, toStringCallsWithFormatProvider[i].FormatProvider))
            {
                allSame = false;
                break;
            }
        }

        if (allSame)
        {
            context.ReportDiagnostic(Rule, operation);
        }
    }

    private static bool AreEquivalentFormatProviders(IOperation first, IOperation second)
    {
        first = first.UnwrapImplicitConversionOperations();
        second = second.UnwrapImplicitConversionOperations();

        // Check if both are property references to the same property
        if (first is IPropertyReferenceOperation firstProp && second is IPropertyReferenceOperation secondProp)
        {
            return SymbolEqualityComparer.Default.Equals(firstProp.Property, secondProp.Property);
        }

        // Check if both are the same constant value
        if (first.ConstantValue.HasValue && second.ConstantValue.HasValue)
        {
            return Equals(first.ConstantValue.Value, second.ConstantValue.Value);
        }

        // Check if both reference the same local variable or parameter
        if (first is ILocalReferenceOperation firstLocal && second is ILocalReferenceOperation secondLocal)
        {
            return SymbolEqualityComparer.Default.Equals(firstLocal.Local, secondLocal.Local);
        }

        if (first is IParameterReferenceOperation firstParam && second is IParameterReferenceOperation secondParam)
        {
            return SymbolEqualityComparer.Default.Equals(firstParam.Parameter, secondParam.Parameter);
        }

        // Check if both are field references to the same field
        if (first is IFieldReferenceOperation firstField && second is IFieldReferenceOperation secondField)
        {
            return SymbolEqualityComparer.Default.Equals(firstField.Field, secondField.Field);
        }

        return false;
    }
}
