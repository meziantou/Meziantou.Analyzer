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
                        // The fix moves the value into a lambda, so it must only use values that a lambda can capture
                        if (!CanBeCapturedByLambda(value))
                            return;

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

    private static bool CanBeCapturedByLambda(IOperation operation)
    {
        foreach (var descendant in operation.DescendantsAndSelf())
        {
            if (CannotBeCapturedByLambda(descendant))
                return false;
        }

        return true;

        static bool CannotBeCapturedByLambda(IOperation operation) => operation switch
        {
            // ref, out and in parameters cannot be used inside a lambda (CS1628)
            IParameterReferenceOperation { Parameter.RefKind: not RefKind.None } => true,

            // ref locals cannot be used inside a lambda (CS8175)
            ILocalReferenceOperation { Local.IsRef: true } => true,

            // Locals and parameters of a ref struct type cannot be used inside a lambda (CS8175)
            IParameterReferenceOperation or ILocalReferenceOperation => operation.Type is { IsRefLikeType: true },

            // The 'this' reference of a struct cannot be captured by a lambda (CS1673)
            IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } => operation.Type is { IsValueType: true },

            _ => false,
        };
    }
}
