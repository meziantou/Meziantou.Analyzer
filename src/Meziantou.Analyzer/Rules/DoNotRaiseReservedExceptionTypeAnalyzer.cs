namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotRaiseReservedExceptionTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotRaiseReservedExceptionType,
        title: "Do not raise reserved exception type",
        messageFormat: "'{0}' is a reserved exception type",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotRaiseReservedExceptionType));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var compilation = ctx.Compilation;
            var reservedExceptionTypes = new List<INamedTypeSymbol>();
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.AccessViolationException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.BadImageFormatException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.CannotUnloadAppDomainException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.DataMisalignedException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.ExecutionEngineException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.IndexOutOfRangeException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.InvalidProgramException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.NullReferenceException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.OutOfMemoryException"));
            reservedExceptionTypes.AddIfNotNull(compilation.GetTypeByMetadataName("System.StackOverflowException"));

            if (reservedExceptionTypes.Count != 0)
            {
                ctx.RegisterOperationAction(_ => Analyze(_, reservedExceptionTypes), OperationKind.Throw);
            }
        });
    }

    private static void Analyze(OperationAnalysisContext context, IEnumerable<INamedTypeSymbol> reservedExceptionTypes)
    {
        var operation = (IThrowOperation)context.Operation;
        if (operation is null || operation.Exception is null)
            return;

        var exceptionType = operation.Exception.GetActualType(context.CancellationToken);
        if (exceptionType is null)
            return;

        if (reservedExceptionTypes.Any(type => exceptionType.IsEqualTo(type) || exceptionType.InheritsFrom(type)))
        {
            context.ReportDiagnostic(Rule, operation, exceptionType.ToDisplayString());
        }
    }
}
