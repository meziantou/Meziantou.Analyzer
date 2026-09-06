namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ObjectGetTypeOnTypeInstanceAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.ObjectGetTypeOnTypeInstance,
        title: "GetType() should not be used on System.Type instances",
        messageFormat: "GetType() should not be used on System.Type instances",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.ObjectGetTypeOnTypeInstance));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var typeSymbol = context.Compilation.GetBestTypeByMetadataName("System.Type");
            if (typeSymbol is null)
                return;

            // System.Type hides Object.GetType() with "public new Type GetType()", so the invocation resolves to
            // Type.GetType() when the instance is statically typed as System.Type, and to Object.GetType() otherwise
            var objectGetTypeSymbol = GetParameterlessGetTypeMethod(context.Compilation.ObjectType);
            var typeGetTypeSymbol = GetParameterlessGetTypeMethod(typeSymbol);
            if (objectGetTypeSymbol is null && typeGetTypeSymbol is null)
                return;

            context.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;

                // The instance of Type.GetType() is statically typed as System.Type, so there is nothing left to check
                if (operation.TargetMethod.IsEqualTo(typeGetTypeSymbol))
                {
                    context.ReportDiagnostic(Rule, operation);
                    return;
                }

                // The instance of Object.GetType() can still hold a System.Type, which only the data flow analysis can tell
                if (operation.TargetMethod.IsEqualTo(objectGetTypeSymbol) && operation.Instance?.GetActualType(context.CancellationToken)?.IsOrInheritsFrom(typeSymbol) is true)
                {
                    context.ReportDiagnostic(Rule, operation);
                }
            }, OperationKind.Invocation);
        });

        static IMethodSymbol? GetParameterlessGetTypeMethod(ITypeSymbol type)
            => type.GetMembers("GetType").OfType<IMethodSymbol>().FirstOrDefault(method => method is { IsStatic: false, Parameters.IsEmpty: true });
    }
}
