namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseLazyInitializerEnsureInitializeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseLazyInitializerEnsureInitialize,
        title: "Use LazyInitializer.EnsureInitialize",
        messageFormat: "Use LazyInitializer.EnsureInitialize",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseLazyInitializerEnsureInitialize));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var interlockedType = compilationContext.Compilation.GetBestTypeByMetadataName("System.Threading.Interlocked");

            compilationContext.RegisterOperationAction(context =>
            {
                var operation = (IInvocationOperation)context.Operation;
                var targetMethod = operation.TargetMethod;

                // Interlocked.CompareExchange(ref _instance, new Sample(), null)
                if (operation.Arguments.Length is 3 && targetMethod.Name is "CompareExchange" && targetMethod.ContainingType.IsEqualTo(interlockedType))
                {
                    if (!operation.Arguments[2].Value.IsNull())
                        return;

                    if (operation.Arguments[0].Value.Type is not { IsReferenceType: true })
                        return;

                    // Interlocked.CompareExchange returns the previous value of the target, whereas
                    // LazyInitializer.EnsureInitialized returns the initialized value
                    if (IsReturnValueUsed(operation))
                        return;

                    var value = operation.Arguments[1].Value.UnwrapImplicitConversions();
                    if (value is IObjectCreationOperation or ILocalReferenceOperation)
                    {
                        context.ReportDiagnostic(Rule, operation);
                    }
                }
            }, OperationKind.Invocation);
        });
    }

    private static bool IsReturnValueUsed(IInvocationOperation operation)
    {
        var parent = operation.Parent;
        if (parent is ISimpleAssignmentOperation { Target: IDiscardOperation })
            return false;

        return parent is not (null or IBlockOperation or IExpressionStatementOperation);
    }
}
