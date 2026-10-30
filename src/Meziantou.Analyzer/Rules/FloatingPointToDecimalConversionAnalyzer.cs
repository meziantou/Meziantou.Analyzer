namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FloatingPointToDecimalConversionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor ConstantRule = new(
        RuleIdentifiers.UseDecimalLiteralInsteadOfConvertingFloatingPointConstant,
        title: "Use a decimal literal instead of converting a floating-point constant to decimal",
        messageFormat: "{0}",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDecimalLiteralInsteadOfConvertingFloatingPointConstant));

    private static readonly DiagnosticDescriptor NonConstantRule = new(
        RuleIdentifiers.ReviewFloatingPointToDecimalConversion,
        title: "Review the conversion of a floating-point value to decimal",
        messageFormat: "Converting a '{0}' value to decimal returns a value that depends on the .NET version, as .NET 11 converts the exact binary value instead of rounding it",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ReviewFloatingPointToDecimalConversion));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ConstantRule, NonConstantRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion, OperationKind.ObjectCreation, OperationKind.Invocation);
        context.RegisterCompilationStartAction(context =>
        {
            // Test frameworks convert the values of these attributes to the type of the parameters of the test method at runtime
            var testDataAttributes = new[]
            {
                context.Compilation.GetTypeByMetadataName("Xunit.InlineDataAttribute"),
                context.Compilation.GetTypeByMetadataName("NUnit.Framework.TestCaseAttribute"),
                context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataRowAttribute"),
            }.WhereNotNull().ToArray();

            if (testDataAttributes.Length > 0)
            {
                context.RegisterOperationAction(context => AnalyzeAttribute(context, testDataAttributes), OperationKind.Attribute);
            }
        });
    }

    private static void AnalyzeConversion(OperationAnalysisContext context)
    {
        if (FloatingPointToDecimalConversionCommon.GetConvertedFloatingPointValue(context.Operation) is not { } value)
            return;

        var typeName = value.Type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        if (FloatingPointToDecimalConversionCommon.IsConstant(value))
        {
            context.ReportDiagnostic(ConstantRule, context.Operation, $"Use a decimal literal instead of converting the '{typeName}' constant to decimal");
        }
        else
        {
            context.ReportDiagnostic(NonConstantRule, context.Operation, typeName);
        }
    }

    private static void AnalyzeAttribute(OperationAnalysisContext context, INamedTypeSymbol[] testDataAttributes)
    {
        if (context.ContainingSymbol is not IMethodSymbol method)
            return;

        var operation = (IAttributeOperation)context.Operation;
        if (operation.Operation is not IObjectCreationOperation { Type: { } attributeType } objectCreation)
            return;

        if (!testDataAttributes.Any(attributeType.IsOrInheritsFrom))
            return;

        // The values of the attribute map to the parameters of the test method by position
        var index = 0;
        foreach (var argument in objectCreation.Arguments)
        {
            if (argument.ArgumentKind is ArgumentKind.DefaultValue)
                continue;

            if (argument.Parameter is { IsParams: true } && argument.Value is IArrayCreationOperation { Initializer: { } initializer })
            {
                foreach (var element in initializer.ElementValues)
                {
                    AnalyzeTestDataValue(context, method, index, element);
                    index++;
                }
            }
            else
            {
                AnalyzeTestDataValue(context, method, index, argument.Value);
                index++;
            }
        }
    }

    private static void AnalyzeTestDataValue(OperationAnalysisContext context, IMethodSymbol method, int index, IOperation value)
    {
        if (index >= method.Parameters.Length)
            return;

        var parameter = method.Parameters[index];
        if (parameter.IsParams || !FloatingPointToDecimalConversionCommon.IsDecimal(parameter.Type))
            return;

        value = value.UnwrapImplicitConversions();
        if (!FloatingPointToDecimalConversionCommon.IsFloatingPoint(value.Type) || !FloatingPointToDecimalConversionCommon.IsConstant(value))
            return;

        var typeName = value.Type!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        context.ReportDiagnostic(ConstantRule, value, $"The '{typeName}' value is converted to the decimal parameter '{parameter.Name}' at runtime, pass the value as a string or use a member data source instead");
    }
}
