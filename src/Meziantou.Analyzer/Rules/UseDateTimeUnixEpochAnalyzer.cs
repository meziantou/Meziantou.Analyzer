using System;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseDateTimeUnixEpochAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_dateTimeRule = new(
        RuleIdentifiers.UseDateTimeUnixEpoch,
        title: "Use DateTime.UnixEpoch",
        messageFormat: "Use DateTime.UnixEpoch",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDateTimeUnixEpoch));

    private static readonly DiagnosticDescriptor s_dateTimeOffsetRule = new(
        RuleIdentifiers.UseDateTimeOffsetUnixEpoch,
        title: "Use DateTimeOffset.UnixEpoch",
        messageFormat: "Use DateTimeOffset.UnixEpoch",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseDateTimeOffsetUnixEpoch));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_dateTimeRule, s_dateTimeOffsetRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(ctx =>
        {
            var dateTimeSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.DateTime");
            var dateTimeOffsetSymbol = ctx.Compilation.GetBestTypeByMetadataName("System.DateTimeOffset");

            if (dateTimeSymbol != null && dateTimeSymbol.GetMembers("UnixEpoch").Length > 0)
            {
                ctx.RegisterOperationAction(ctx => AnalyzeDateTimeObjectCreation(ctx, dateTimeSymbol), OperationKind.ObjectCreation);
            }

            if (dateTimeSymbol != null && dateTimeOffsetSymbol != null && dateTimeOffsetSymbol.GetMembers("UnixEpoch").Length > 0)
            {
                ctx.RegisterOperationAction(ctx => AnalyzeDateTimeOffsetObjectCreation(ctx, dateTimeSymbol, dateTimeOffsetSymbol), OperationKind.ObjectCreation);
            }
        });
    }

    private static void AnalyzeDateTimeObjectCreation(OperationAnalysisContext context, ITypeSymbol dateTimeSymbol)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        if (IsDateTimeUnixEpoch(operation, context.Compilation, dateTimeSymbol))
        {
            context.ReportDiagnostic(s_dateTimeRule, operation);
        }
    }
    private static void AnalyzeDateTimeOffsetObjectCreation(OperationAnalysisContext context, ITypeSymbol dateTimeSymbol, ITypeSymbol dateTimeOffsetSymbol)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        if (IsDateTimeOffsetUnixEpoch())
        {
            context.ReportDiagnostic(s_dateTimeOffsetRule, operation);
        }

        bool IsDateTimeOffsetUnixEpoch()
        {
            if (!operation.Type.IsEqualTo(dateTimeOffsetSymbol))
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
                if (memberReference.Member.Name == "UnixEpoch" && memberReference.Member.ContainingType.IsEqualTo(dateTimeSymbol))
                    return true;
            }

            return false;
        }
    }

    private static bool IsDateTimeUnixEpoch(IObjectCreationOperation operation, Compilation compilation, ITypeSymbol dateTimeSymbol)
    {
        if (!operation.Type.IsEqualTo(dateTimeSymbol))
            return false;

        if (operation.Arguments.Length == 1)
        {
            if (ArgumentsEquals(operation.Arguments.AsSpan(), [621355968000000000L]))
                return true;
        }
        else if (operation.Arguments.Length == 2)
        {
            if (ArgumentsEquals(operation.Arguments.AsSpan(0, 1), [621355968000000000L]) && IsDateTimeKindUtc(compilation, operation.Arguments[1]))
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
            if (ArgumentsEquals(operation.Arguments.AsSpan(0, 6), [1970, 1, 1, 0, 0, 0]) && IsDateTimeKindUtc(compilation, operation.Arguments[6]))
                return true;
        }

        return false;
    }

    private static bool IsDateTimeKindUtc(Compilation compilation, IArgumentOperation argument)
    {
        var dateTimeKindSymbol = compilation.GetBestTypeByMetadataName("System.DateTimeKind");
        if (dateTimeKindSymbol == null)
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

    private static bool IsTimeSpanZero(IArgumentOperation operation)
    {
        return TimeSpanOperation.GetMilliseconds(operation.Value) is 0L;
    }
}
