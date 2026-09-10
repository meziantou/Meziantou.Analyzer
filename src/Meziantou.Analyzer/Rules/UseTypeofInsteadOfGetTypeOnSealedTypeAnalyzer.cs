namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseTypeofInsteadOfGetTypeOnSealedTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseTypeofInsteadOfGetTypeOnSealedType,
        title: "Use 'typeof' instead of 'GetType()' when the type is sealed",
        messageFormat: "Use 'typeof({0})' instead of 'GetType()' as '{0}' cannot have derived types",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseTypeofInsteadOfGetTypeOnSealedType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(context =>
        {
            var operation = (IInvocationOperation)context.Operation;
            if (UseTypeofInsteadOfGetTypeOnSealedTypeCommon.GetKnownType(operation) is not { } type)
                return;

            context.ReportDiagnostic(Rule, operation, type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }, OperationKind.Invocation);
    }
}
