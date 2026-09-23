namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseReadOnlyStructForRefReadOnlyParametersAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseReadOnlyStructForRefReadOnlyParameters,
        title: "Use readonly struct for in or ref readonly parameter",
        messageFormat: "Use readonly struct for in or ref readonly parameter",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseReadOnlyStructForRefReadOnlyParameters));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(context =>
        {
            var parameter = (IParameterSymbol)context.Symbol;
            if (!IsValidParameter(parameter, context.Compilation.Assembly))
            {
                context.ReportDiagnostic(Rule, parameter);
            }

        }, SymbolKind.Parameter);

        context.RegisterOperationAction(context =>
        {
            var operation = (ILocalFunctionOperation)context.Operation;
            var symbol = operation.Symbol;
            foreach (var parameter in symbol.Parameters)
            {
                if (!IsValidParameter(parameter, context.Compilation.Assembly))
                {
                    context.ReportDiagnostic(Rule, parameter);
                }
            }

        }, OperationKind.LocalFunction);

        context.RegisterOperationAction(ctx =>
        {
            var operation = (IArgumentOperation)ctx.Operation;
            var parameter = operation.Parameter;
            if (parameter is null)
                return;

            // Do not report non-generic types as they are reported by SymbolAction
            if (SymbolEqualityComparer.Default.Equals(parameter.OriginalDefinition.Type, parameter.Type))
                return;

            if (!IsValidParameter(parameter, ctx.Compilation.Assembly))
            {
                ctx.ReportDiagnostic(Rule, operation);
            }
        }, OperationKind.Argument);
    }

    private static bool IsValidParameter(IParameterSymbol parameter, IAssemblySymbol currentAssembly)
    {
        if (parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter)
        {
            // Only the structs declared in the current assembly can be made readonly. Enums cannot be readonly structs.
            if (parameter.Type is INamedTypeSymbol { TypeKind: TypeKind.Struct, IsReadOnly: false } namedTypeSymbol &&
                namedTypeSymbol.ContainingAssembly.IsEqualTo(currentAssembly))
            {
                return false;
            }
        }

        return true;
    }
}