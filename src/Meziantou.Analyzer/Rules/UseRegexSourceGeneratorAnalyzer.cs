namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class UseRegexSourceGeneratorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RegexSourceGeneratorRule = new(
        RuleIdentifiers.UseRegexSourceGenerator,
        title: "Use the Regex source generator",
        messageFormat: "Use the Regex source generator",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseRegexSourceGenerator));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RegexSourceGeneratorRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationAction(ctx => analyzerContext.AnalyzeObjectCreation(ctx), OperationKind.ObjectCreation);
            ctx.RegisterOperationAction(ctx => analyzerContext.AnalyzeInvocation(ctx), OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        // The parameters lifted to the GeneratedRegex attribute
        private const string PatternParameterName = "pattern";
        private const string OptionsParameterName = "options";
        private const string MatchTimeoutParameterName = "matchTimeout";

        private readonly TimeSpanOperation _timeSpanOperation = new(compilation);
        private readonly ITypeSymbol? _regexSymbol = compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex");
        private readonly ITypeSymbol? _regexGeneratorAttributeSymbol = compilation.GetTypeByMetadataName("System.Text.RegularExpressions.GeneratedRegexAttribute");
        private readonly ITypeSymbol? _timespanSymbol = compilation.GetTypeByMetadataName("System.TimeSpan");

        private bool CanReport(IOperation operation)
        {
            if (_regexSymbol is null)
                return false;

            if (_regexGeneratorAttributeSymbol is null)
                return false;

            // https://github.com/dotnet/runtime/pull/66111
            if (!operation.GetCSharpLanguageVersion().IsCSharp10OrGreater())
                return false;

            return true;
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            if (!CanReport(context.Operation))
                return;

            var op = (IObjectCreationOperation)context.Operation;
            if (!op.Type.IsEqualTo(_regexSymbol))
                return;

            // Regex(string pattern)
            // Regex(string pattern, RegexOptions options)
            // Regex(string pattern, RegexOptions options, TimeSpan matchTimeout)
            var properties = TryCreateProperties(op.Arguments);
            if (properties is null)
                return;

            context.ReportDiagnostic(RegexSourceGeneratorRule, properties, op);
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (!CanReport(context.Operation))
                return;

            var op = (IInvocationOperation)context.Operation;
            var method = op.TargetMethod;
            if (!method.IsStatic || !method.ContainingType.IsEqualTo(_regexSymbol))
                return;

            // IsMatch/Match/Matches/Split(string input, string pattern[, RegexOptions options[, TimeSpan matchTimeout]])
            // Replace(string input, string pattern, string replacement[, RegexOptions options[, TimeSpan matchTimeout]])
            // Replace(string input, string pattern, MatchEvaluator evaluator[, RegexOptions options[, TimeSpan matchTimeout]])
            if (method.Name is not ("IsMatch" or "Match" or "Matches" or "Split" or "Replace"))
                return;

            var properties = TryCreateProperties(op.Arguments);
            if (properties is null)
                return;

            context.ReportDiagnostic(RegexSourceGeneratorRule, properties, op);
        }

        /// <summary>
        /// Computes the diagnostic properties describing the arguments the code fixer must lift to the
        /// <c>GeneratedRegex</c> attribute, or <see langword="null"/> when the operation cannot be converted.
        /// </summary>
        /// <remarks>
        /// The arguments are located from <see cref="IArgumentOperation.Parameter"/> instead of their position in
        /// <paramref name="arguments"/>, as reordered named arguments are listed in evaluation order.
        /// </remarks>
        private ImmutableDictionary<string, string?>? TryCreateProperties(ImmutableArray<IArgumentOperation> arguments)
        {
            var patternIndex = GetArgumentIndex(arguments, PatternParameterName);
            if (patternIndex is null)
                return null;

            var optionsIndex = GetArgumentIndex(arguments, OptionsParameterName);
            var timeoutIndex = GetArgumentIndex(arguments, MatchTimeoutParameterName);

            // An overload with an unknown parameter cannot be converted, as the parameter would be silently dropped
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i == patternIndex || i == optionsIndex || i == timeoutIndex)
                    continue;

                if (arguments[i].Parameter?.Name is not ("input" or "replacement" or "evaluator"))
                    return null;
            }

            if (!IsConstant(arguments[patternIndex.Value]))
                return null;

            if (optionsIndex is not null && !IsConstant(arguments[optionsIndex.Value]))
                return null;

            if (timeoutIndex is not null && !IsConstant(arguments[timeoutIndex.Value]))
                return null;

            return ImmutableDictionary.CreateRange(
            [
                new KeyValuePair<string, string?>(UseRegexSourceGeneratorAnalyzerCommon.PatternIndexName, patternIndex.Value.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>(UseRegexSourceGeneratorAnalyzerCommon.RegexOptionsIndexName, optionsIndex?.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>(UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutIndexName, timeoutIndex?.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string?>(UseRegexSourceGeneratorAnalyzerCommon.RegexTimeoutName, timeoutIndex is null ? null : _timeSpanOperation.GetMilliseconds(arguments[timeoutIndex.Value].Value)?.ToString(CultureInfo.InvariantCulture)),
            ]);
        }

        private static int? GetArgumentIndex(ImmutableArray<IArgumentOperation> arguments, string parameterName)
        {
            for (var i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].Parameter?.Name == parameterName)
                    return i;
            }

            return null;
        }

        private bool IsConstant(IArgumentOperation argumentOperation)
        {
            var valueOperation = argumentOperation.Value;
            if (valueOperation.ConstantValue.HasValue)
                return true;

            if (valueOperation.Type.IsEqualTo(_timespanSymbol))
            {
                // GeneratedRegex only accepts an infinite or strictly positive match timeout, so a Regex built with
                // any other value cannot be converted: the source generator would reject the generated attribute.
                // Its timeout is an Int32 number of milliseconds, so a longer duration cannot be converted either.
                const long Infinite = -1;
                var milliseconds = _timeSpanOperation.GetMilliseconds(valueOperation);
                return milliseconds is Infinite or (> 0 and <= int.MaxValue);
            }

            return false;
        }
    }
}
