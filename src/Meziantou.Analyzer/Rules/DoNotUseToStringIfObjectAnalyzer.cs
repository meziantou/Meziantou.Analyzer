namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseToStringIfObjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseToStringIfObject,
        title: "Do not call ToString() when the type falls back to object.ToString()",
        messageFormat: "ToString on '{0}' will use the default object.ToString",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseToStringIfObject));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);

            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInterpolation, OperationKind.InterpolatedString);
            context.RegisterOperationAction(AnalyzerContext.AnalyzeAdd, OperationKind.Binary);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveFormattingContext = new(compilation);

        public IMethodSymbol? ObjectToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_Object).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);
        public IMethodSymbol? ValueTypeToStringSymbol { get; } = compilation.GetSpecialType(SpecialType.System_ValueType).GetMembers("ToString").OfType<IMethodSymbol>().FirstOrDefault(member => member.Parameters.Length == 0);

        public void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            var operation = (IInterpolatedStringOperation)context.Operation;
            if (IsCustomInterpolatedStringHandler(operation))
                return;

            foreach (var part in operation.Parts)
            {
                if (part is IInterpolationOperation content)
                {
                    AnalyzeExpression(context, content.Expression, context.CancellationToken);
                }
                else if (part is IInterpolatedStringAppendOperation { AppendCall: IInvocationOperation appendCall })
                {
                    if (!ShouldAnalyzeInterpolatedStringHandler(appendCall.TargetMethod.ContainingType))
                        continue;

                    if (appendCall.Arguments is not [{ Value: var content2 }])
                        continue;

                    AnalyzeExpression(context, content2, context.CancellationToken);
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation.Instance?.Type?.IsAnonymousType is true)
                return;

            if (!IsDefaultToString(operation.TargetMethod))
                return;

            if (operation.Instance is null)
                return;

            var actualType = operation.Instance.GetActualType(context.CancellationToken);
            if (actualType is null)
                return;

            if (CultureSensitiveFormattingContext.UsesObjectToString(actualType, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule, operation, actualType.ToDisplayString());
            }
        }

        internal static void AnalyzeAdd(OperationAnalysisContext context)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (!operation.Type.IsString())
                return;

            AnalyzeExpression(context, operation.LeftOperand, context.CancellationToken);
            AnalyzeExpression(context, operation.RightOperand, context.CancellationToken);
        }

        private static void AnalyzeExpression(DiagnosticReporter reporter, IOperation operation, CancellationToken cancellationToken)
        {
            var actualType = operation.GetActualType(cancellationToken);
            if (actualType is null)
                return;

            if (actualType.IsAnonymousType)
                return;

            if (CultureSensitiveFormattingContext.UsesObjectToString(actualType, cancellationToken))
            {
                reporter.ReportDiagnostic(Rule, operation, [actualType.ToDisplayString()]);
            }
        }

        private bool IsDefaultToString(IMethodSymbol method)
        {
            return method.IsEqualTo(ObjectToStringSymbol) || method.IsEqualTo(ValueTypeToStringSymbol);
        }

        private bool ShouldAnalyzeInterpolatedStringHandler(INamedTypeSymbol containingType)
        {
            if (_cultureSensitiveFormattingContext.IsInterpolatedStringHandlerThatFormatsStringValues(containingType))
                return true;

            return _cultureSensitiveFormattingContext.IsInterpolatedStringHandlerType(containingType);
        }

        private bool IsCustomInterpolatedStringHandler(IInterpolatedStringOperation operation)
        {
            if (_cultureSensitiveFormattingContext.InterpolatedStringHandlerAttributeSymbol is null)
                return false;

            if (operation.Type is { } operationType &&
                operationType.HasAttribute(_cultureSensitiveFormattingContext.InterpolatedStringHandlerAttributeSymbol) &&
                !_cultureSensitiveFormattingContext.IsInterpolatedStringHandlerThatFormatsStringValues(operationType))
                return true;

            for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is IArgumentOperation { Parameter.Type: var parameterType } &&
                    parameterType.HasAttribute(_cultureSensitiveFormattingContext.InterpolatedStringHandlerAttributeSymbol) &&
                    !_cultureSensitiveFormattingContext.IsInterpolatedStringHandlerThatFormatsStringValues(parameterType))
                    return true;

                if (parent is IConversionOperation { Type: var conversionType } &&
                    conversionType?.HasAttribute(_cultureSensitiveFormattingContext.InterpolatedStringHandlerAttributeSymbol) == true &&
                    !_cultureSensitiveFormattingContext.IsInterpolatedStringHandlerThatFormatsStringValues(conversionType))
                    return true;
            }

            return false;
        }
    }
}
