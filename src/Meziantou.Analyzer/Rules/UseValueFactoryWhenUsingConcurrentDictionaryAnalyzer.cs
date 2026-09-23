namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseValueFactoryWhenUsingConcurrentDictionaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseValueFactoryWhenUsingConcurrentDictionary,
        title: "Use a value factory to compute the value only when the key is not in the ConcurrentDictionary",
        messageFormat: "The value is computed even when the key is already in the dictionary, use an overload with a value factory",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseValueFactoryWhenUsingConcurrentDictionary));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            var concurrentDictionarySymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Concurrent.ConcurrentDictionary`2");
            if (concurrentDictionarySymbol is null)
                return;

            context.RegisterOperationAction(context => AnalyzeInvocation(context, concurrentDictionarySymbol), OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol concurrentDictionarySymbol)
    {
        var operation = (IInvocationOperation)context.Operation;
        if (operation.TargetMethod.Name is not ("GetOrAdd" or "AddOrUpdate"))
            return;

        if (!operation.TargetMethod.ContainingType.OriginalDefinition.IsEqualTo(concurrentDictionarySymbol))
            return;

        // GetOrAdd(TKey key, TValue value) and AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        // have an overload that takes a Func<TKey, TValue> to compute the value only when the key is not in the dictionary
        var valueTypeParameter = concurrentDictionarySymbol.TypeParameters[1];
        foreach (var argument in operation.Arguments)
        {
            if (argument.Parameter is null || !argument.Parameter.OriginalDefinition.Type.IsEqualTo(valueTypeParameter))
                continue;

            if (IsCheapToEvaluate(argument.Value))
                continue;

            context.ReportDiagnostic(Rule, argument.Value);
        }
    }

    private static bool IsCheapToEvaluate(IOperation operation)
    {
        if (operation.ConstantValue.HasValue)
            return true;

        return operation switch
        {
            ILocalReferenceOperation or IParameterReferenceOperation or IInstanceReferenceOperation => true,
            IDefaultValueOperation or ITypeOfOperation or ISizeOfOperation => true,
            IAnonymousFunctionOperation or IDelegateCreationOperation => true,
            IConditionalAccessInstanceOperation => true,

            // Moving a throw expression to a factory would change when the exception is thrown
            IThrowOperation => true,
            IFieldReferenceOperation { Instance: var instance } => instance is null || IsCheapToEvaluate(instance),

            // Properties are expected to be cheap to evaluate, unlike indexers
            IPropertyReferenceOperation { Arguments.IsEmpty: true, Instance: var instance } => instance is null || IsCheapToEvaluate(instance),
            IArrayElementReferenceOperation arrayElement => IsCheapToEvaluate(arrayElement.ArrayReference) && arrayElement.Indices.All(IsCheapToEvaluate),
            IConversionOperation { OperatorMethod: null } conversion => IsCheapToEvaluate(conversion.Operand),

            // The built-in operators returning a reference type, such as the string concatenation, allocate a new instance
            IUnaryOperation { OperatorMethod: null, Type.IsValueType: true } unary => IsCheapToEvaluate(unary.Operand),
            IBinaryOperation { OperatorMethod: null, Type.IsValueType: true } binary => IsCheapToEvaluate(binary.LeftOperand) && IsCheapToEvaluate(binary.RightOperand),
            IConditionalOperation conditional => IsCheapToEvaluate(conditional.Condition) && IsCheapToEvaluate(conditional.WhenTrue) && (conditional.WhenFalse is null || IsCheapToEvaluate(conditional.WhenFalse)),
            ICoalesceOperation coalesce => IsCheapToEvaluate(coalesce.Value) && IsCheapToEvaluate(coalesce.WhenNull),
            IConditionalAccessOperation conditionalAccess => IsCheapToEvaluate(conditionalAccess.Operation) && IsCheapToEvaluate(conditionalAccess.WhenNotNull),
            ITupleOperation tuple => tuple.Elements.All(IsCheapToEvaluate),

            // new MyStruct() without a user-defined constructor is equivalent to default(MyStruct)
            IObjectCreationOperation { Type.IsValueType: true, Arguments.IsEmpty: true, Initializer: null, Constructor: null or { IsImplicitlyDeclared: true } } => true,
            _ => false,
        };
    }
}
