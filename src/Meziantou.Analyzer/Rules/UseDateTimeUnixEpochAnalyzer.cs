using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

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
        private readonly ITypeSymbol? _dateTimeSymbol = compilation.GetBestTypeByMetadataName("System.DateTime");
        private readonly ITypeSymbol? _dateTimeOffsetSymbol = compilation.GetBestTypeByMetadataName("System.DateTimeOffset");
        private readonly ITypeSymbol? _dateTimeKindSymbol = compilation.GetBestTypeByMetadataName("System.DateTimeKind");

        public bool HasDateTimeUnixEpoch => _dateTimeSymbol is not null && _dateTimeSymbol.GetMembers("UnixEpoch").Length > 0;
        public bool HasDateTimeOffsetUnixEpoch => _dateTimeOffsetSymbol is not null && _dateTimeOffsetSymbol.GetMembers("UnixEpoch").Length > 0;

        public void AnalyzeDateTimeObjectCreation(OperationAnalysisContext context)
        {
            var operation = (IObjectCreationOperation)context.Operation;
            if (IsDateTimeUnixEpoch(operation))
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
                    if (ArgumentsEquals(operation.Arguments.AsSpan(), [621355968000000000L]))
                        return true;

                    if (IsUnixEpochProperty(operation.Arguments[0]))
                        return true;
                }
                else if (operation.Arguments.Length == 2)
                {
                    if (ArgumentsEquals(operation.Arguments.AsSpan(0, 1), [621355968000000000L]) && IsTimeSpanZero(operation.Arguments[1]))
                        return true;

                    if (IsUnixEpochProperty(operation.Arguments[0]) && IsTimeSpanZero(operation.Arguments[1]))
                        return true;
                }
                else if (operation.Arguments.Length == 7)
                {
                    if (ArgumentsEquals(operation.Arguments.AsSpan(0, 6), [1970, 1, 1, 0, 0, 0]) && IsTimeSpanZero(operation.Arguments[6]))
                        return true;
                }
                else if (operation.Arguments.Length == 8)
                {
                    if (ArgumentsEquals(operation.Arguments.AsSpan(0, 7), [1970, 1, 1, 0, 0, 0, 0]) && IsTimeSpanZero(operation.Arguments[7]))
                        return true;
                }
                else if (operation.Arguments.Length == 9)
                {
                    if (ArgumentsEquals(operation.Arguments.AsSpan(0, 8), [1970, 1, 1, 0, 0, 0, 0, 0]) && IsTimeSpanZero(operation.Arguments[8]))
                        return true;
                }

                return false;
            }

            bool IsUnixEpochProperty(IArgumentOperation argumentOperation)
            {
                if (argumentOperation.Value is IMemberReferenceOperation memberReference)
                {
                    if (memberReference.Member.Name == "UnixEpoch" && memberReference.Member.ContainingType.IsEqualTo(_dateTimeSymbol))
                        return true;
                }

                return false;
            }
        }

        private bool IsDateTimeUnixEpoch(IObjectCreationOperation operation)
        {
            if (!operation.Type.IsEqualTo(_dateTimeSymbol))
                return false;

            if (operation.Arguments.Length == 1)
            {
                if (ArgumentsEquals(operation.Arguments.AsSpan(), [621355968000000000L]))
                    return true;
            }
            else if (operation.Arguments.Length == 2)
            {
                if (ArgumentsEquals(operation.Arguments.AsSpan(0, 1), [621355968000000000L]) && IsDateTimeKindUtc(operation.Arguments[1]))
                    return true;
            }
            else if (operation.Arguments.Length == 3)
            {
                if (ArgumentsEquals(operation.Arguments.AsSpan(), [1970, 1, 1]))
                    return true;
            }
            else if (operation.Arguments.Length == 6)
            {
                if (ArgumentsEquals(operation.Arguments.AsSpan(), [1970, 1, 1, 0, 0, 0]))
                    return true;
            }
            else if (operation.Arguments.Length == 7)
            {
                if (ArgumentsEquals(operation.Arguments.AsSpan(0, 6), [1970, 1, 1, 0, 0, 0]) && IsDateTimeKindUtc(operation.Arguments[6]))
                    return true;
            }

            return false;
        }

        private bool IsDateTimeKindUtc(IArgumentOperation argument)
        {
            if (_dateTimeKindSymbol is null)
                return false;

            return argument.Value.ConstantValue.HasValue && (DateTimeKind)argument.Value.ConstantValue.Value! == DateTimeKind.Utc;
        }

        private static bool ArgumentsEquals(ReadOnlySpan<IArgumentOperation> arguments, object[] expectedValues)
        {
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                if (!argument.Value.ConstantValue.HasValue)
                    return false;

                if (!Equals(argument.Value.ConstantValue.Value, expectedValues[i]))
                    return false;
            }

            return true;
        }

        private bool IsTimeSpanZero(IArgumentOperation operation)
        {
            return _timeSpanOperation.GetMilliseconds(operation.Value) is 0L;
        }
    }
}