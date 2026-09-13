namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseDateTimeUnixEpochAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DateTimeRule = new(
        RuleIdentifiers.UseDateTimeUnixEpoch,
        title: "Use DateTime.UnixEpoch",
        messageFormat: "Use DateTime.UnixEpoch",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDateTimeUnixEpoch));

    private static readonly DiagnosticDescriptor DateTimeOffsetRule = new(
        RuleIdentifiers.UseDateTimeOffsetUnixEpoch,
        title: "Use DateTimeOffset.UnixEpoch",
        messageFormat: "Use DateTimeOffset.UnixEpoch",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDateTimeOffsetUnixEpoch));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DateTimeRule, DateTimeOffsetRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            if (analyzerContext.HasDateTimeUnixEpoch)
            {
                ctx.RegisterOperationAction(ctx => analyzerContext.AnalyzeDateTimeObjectCreation(ctx), OperationKind.ObjectCreation);
            }

            if (analyzerContext.HasDateTimeOffsetUnixEpoch)
            {
                ctx.RegisterOperationAction(ctx => analyzerContext.AnalyzeDateTimeOffsetObjectCreation(ctx), OperationKind.ObjectCreation);
            }
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly TimeSpanOperation _timeSpanOperation = new(compilation);
        private readonly ITypeSymbol? _dateTimeSymbol = compilation.GetTypeByMetadataName("System.DateTime");
        private readonly ITypeSymbol? _dateTimeOffsetSymbol = compilation.GetTypeByMetadataName("System.DateTimeOffset");
        private readonly ITypeSymbol? _dateTimeKindSymbol = compilation.GetTypeByMetadataName("System.DateTimeKind");

        public bool HasDateTimeUnixEpoch => _dateTimeSymbol is not null && _dateTimeSymbol.GetMembers("UnixEpoch").Length > 0;
        public bool HasDateTimeOffsetUnixEpoch => _dateTimeOffsetSymbol is not null && _dateTimeOffsetSymbol.GetMembers("UnixEpoch").Length > 0;

        public void AnalyzeDateTimeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (IsDateTimeUnixEpoch(operation, context.CancellationToken))
            {
                context.ReportDiagnostic(DateTimeRule, operation);
            }
        }

        public void AnalyzeDateTimeOffsetObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (IsDateTimeOffsetUnixEpoch())
            {
                context.ReportDiagnostic(DateTimeOffsetRule, operation);
            }

            bool IsDateTimeOffsetUnixEpoch()
            {
                if (!operation.Type.IsEqualTo(_dateTimeOffsetSymbol))
                    return false;

                if (operation.Arguments.Length == 1)
                {
                    if (ArgumentsEquals(operation, [621355968000000000L], context.CancellationToken))
                        return true;

                    if (IsUnixEpochProperty(GetArgument(operation, 0)))
                        return true;
                }
                else if (operation.Arguments.Length == 2)
                {
                    if (ArgumentsEquals(operation, [621355968000000000L], context.CancellationToken) && IsTimeSpanZero(GetArgument(operation, 1)))
                        return true;

                    if (IsUnixEpochProperty(GetArgument(operation, 0)) && IsTimeSpanZero(GetArgument(operation, 1)))
                        return true;
                }
                else if (operation.Arguments.Length == 7)
                {
                    if (ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0], context.CancellationToken) && IsTimeSpanZero(GetArgument(operation, 6)))
                        return true;
                }
                else if (operation.Arguments.Length == 8)
                {
                    if (ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0, 0], context.CancellationToken) && IsTimeSpanZero(GetArgument(operation, 7)))
                        return true;
                }
                else if (operation.Arguments.Length == 9)
                {
                    if (ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0, 0, 0], context.CancellationToken) && IsTimeSpanZero(GetArgument(operation, 8)))
                        return true;
                }

                return false;
            }

            bool IsUnixEpochProperty(IArgumentOperation? argumentOperation)
            {
                if (argumentOperation?.Value is IMemberReferenceOperation memberReference)
                {
                    if (memberReference.Member.Name == "UnixEpoch" && memberReference.Member.ContainingType.IsEqualTo(_dateTimeSymbol))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Determines whether the operation creates a <see cref="DateTime"/> equal to <c>DateTime.UnixEpoch</c>, including its <see cref="DateTimeKind.Utc"/> kind.
        /// The constructors that do not take a <see cref="DateTimeKind"/> create a <see cref="DateTimeKind.Unspecified"/> value, so replacing them would change the behavior.
        /// </summary>
        private bool IsDateTimeUnixEpoch(IObjectCreationOperation operation, CancellationToken cancellationToken)
        {
            if (!operation.Type.IsEqualTo(_dateTimeSymbol))
                return false;

            return operation.Arguments.Length switch
            {
                2 => ArgumentsEquals(operation, [621355968000000000L], cancellationToken) && IsDateTimeKindUtc(GetArgument(operation, 1), cancellationToken),
                7 => ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0], cancellationToken) && IsDateTimeKindUtc(GetArgument(operation, 6), cancellationToken),
                8 => ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0, 0], cancellationToken) && IsDateTimeKindUtc(GetArgument(operation, 7), cancellationToken),
                9 => ArgumentsEquals(operation, [1970, 1, 1, 0, 0, 0, 0, 0], cancellationToken) && IsDateTimeKindUtc(GetArgument(operation, 8), cancellationToken),
                _ => false,
            };
        }

        private bool IsDateTimeKindUtc(IArgumentOperation? argument, CancellationToken cancellationToken)
        {
            if (_dateTimeKindSymbol is null || argument is null)
                return false;

            var parameter = argument.Parameter;
            if (parameter is null || !parameter.Type.IsEqualTo(_dateTimeKindSymbol))
                return false;

            return argument.Value.TryGetConstantValue(out var value, cancellationToken) && value is int intValue && intValue == (int)DateTimeKind.Utc;
        }

        /// <summary>
        /// Gets the argument for the parameter at the given position, whatever the order of the arguments in the source code.
        /// </summary>
        private static IArgumentOperation? GetArgument(IObjectCreationOperation operation, int parameterOrdinal)
        {
            foreach (var argument in operation.Arguments)
            {
                if (argument.Parameter?.Ordinal == parameterOrdinal)
                    return argument;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the arguments of the leading parameters are the constants <paramref name="expectedValues"/>,
        /// whatever the order of the arguments in the source code. The arguments of the remaining parameters are not validated.
        /// </summary>
        private static bool ArgumentsEquals(IObjectCreationOperation operation, object[] expectedValues, CancellationToken cancellationToken)
        {
            var matchedParameters = 0;
            foreach (var argument in operation.Arguments)
            {
                var parameter = argument.Parameter;
                if (parameter is null || parameter.Ordinal >= expectedValues.Length)
                    continue;

                if (!argument.Value.TryGetConstantValue(out var value, cancellationToken))
                    return false;

                if (!Equals(value, expectedValues[parameter.Ordinal]))
                    return false;

                matchedParameters++;
            }

            return matchedParameters == expectedValues.Length;
        }

        private bool IsTimeSpanZero(IArgumentOperation? operation)
        {
            return operation is not null && _timeSpanOperation.GetMilliseconds(operation.Value) is 0L;
        }
    }
}
