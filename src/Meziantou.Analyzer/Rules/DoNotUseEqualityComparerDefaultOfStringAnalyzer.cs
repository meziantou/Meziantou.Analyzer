namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseEqualityComparerDefaultOfStringAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString,
        title: "Use an explicit StringComparer when possible",
        messageFormat: "Use an overload of '{0}' with a StringComparer",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Comparer<string>.Default is culture-sensitive, and EqualityComparer<string>.Default is ordinal.
            // Both hide the comparison actually used, so an explicit StringComparer is preferred.
            var equalityComparerStringSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.EqualityComparer`1")?.Construct(stringType);
            var comparerStringSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.Comparer`1")?.Construct(stringType);
            if (equalityComparerStringSymbol is null && comparerStringSymbol is null)
                return;

            compilationContext.RegisterOperationAction(context => Analyze(context, equalityComparerStringSymbol, comparerStringSymbol), OperationKind.PropertyReference);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ITypeSymbol? equalityComparerStringSymbol, ITypeSymbol? comparerStringSymbol)
    {
        var operation = (IPropertyReferenceOperation)context.Operation;
        if (!string.Equals(operation.Member.Name, nameof(EqualityComparer<>.Default), StringComparison.Ordinal))
            return;

        var containingType = operation.Member.ContainingType;
        if (containingType.IsEqualTo(equalityComparerStringSymbol) || containingType.IsEqualTo(comparerStringSymbol))
        {
            if (operation.IsInNameofOperation())
                return;

            context.ReportDiagnostic(Rule, operation, operation.Member.Name);
        }
    }
}
