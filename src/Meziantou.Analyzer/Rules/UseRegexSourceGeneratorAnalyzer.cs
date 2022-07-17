using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseRegexSourceGeneratorAnalyzer : DiagnosticAnalyzer
{
    internal const string PatternIndexName = "PatternIndex";
    internal const string RegexOptionsIndexName = "RegexOptionsIndex";
    internal const string RegexTimeoutIndexName = "RegexTimeoutIndex";
    internal const string RegexTimeoutName = "RegexTimeout";

    private static readonly DiagnosticDescriptor s_regexSourceGeneratorRule = new(
        RuleIdentifiers.UseRegexSourceGenerator,
        title: "Use the Regex source generator",
        messageFormat: "Use the Regex source generator",
        RuleCategories.Performance,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseRegexSourceGenerator));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_regexSourceGeneratorRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static bool CanReport(IOperation operation)
    {
        var compilation = operation.SemanticModel!.Compilation;
        var regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
        if (regexSymbol == null)
            return false;

        var regexGeneratorAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexGeneratorAttribute");
        if (regexGeneratorAttributeSymbol == null)
            return false;

        // https://github.com/dotnet/runtime/pull/66111
        if (operation.GetCSharpLanguageVersion().IsCSharp10OrBellow())
            return false;

        return true;
    }

    private void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        if (!CanReport(context.Operation))
            return;

        var op = (IObjectCreationOperation)context.Operation;
        if (!op.Type.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex")))
            return;

        foreach (var arg in op.Arguments)
        {
            if (!IsConstant(arg))
                return;
        }

        var properties = ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string?>(PatternIndexName, "0"),
            new KeyValuePair<string, string?>(RegexOptionsIndexName, op.Arguments.Length > 1 ? "1" : null),
            new KeyValuePair<string, string?>(RegexTimeoutIndexName, op.Arguments.Length > 2 ? "2" : null),
            new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 2 ? GetMilliseconds(op.Arguments[2].Value)?.ToString(CultureInfo.InvariantCulture) : null),
        });

        context.ReportDiagnostic(s_regexSourceGeneratorRule, properties, op);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (!CanReport(context.Operation))
            return;

        var op = (IInvocationOperation)context.Operation;
        var method = op.TargetMethod;
        if (!method.IsStatic || !method.ContainingType.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex")))
            return;

        if (method.Name is "IsMatch" or "Match" or "Matches" or "Split")
        {
            // IsMatch(string _, string)
            // IsMatch(string _, string, RegexOptions)
            // IsMatch(string _, string, RegexOptions, TimeSpan)

            // Match(string _, string)
            // Match(string _, string, RegexOptions)
            // Match(string _, string, RegexOptions, TimeSpan)

            // Matches(string _, string)
            // Matches(string _, string, RegexOptions)
            // Matches(string _, string, RegexOptions, TimeSpan)

            // Split(string _, string)
            // Split(string _, string, RegexOptions)
            // Split(string _, string, RegexOptions, TimeSpan)

            for (var i = 1; i < op.Arguments.Length; i++)
            {
                if (!IsConstant(op.Arguments[i]))
                    return;
            }

            var properties = ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string?>(PatternIndexName, "1"),
                new KeyValuePair<string, string?>(RegexOptionsIndexName, op.Arguments.Length > 2 ? "2" : null),
                new KeyValuePair<string, string?>(RegexTimeoutIndexName, op.Arguments.Length > 3 ? "3" : null),
                new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 3 ? GetMilliseconds(op.Arguments[3].Value)?.ToString(CultureInfo.InvariantCulture) : null),
            });

            context.ReportDiagnostic(s_regexSourceGeneratorRule, properties, op);
        }
        else if (method.Name is "Replace")
        {
            // Replace(string _, string, MatchEvaluator _, RegexOptions, TimeSpan)
            // Replace(string _, string, MatchEvaluator _, RegexOptions)
            // Replace(string _, string, MatchEvaluator _)
            // Replace(string _, string, string _, RegexOptions, TimeSpan)
            // Replace(string _, string, string _, RegexOptions)
            // Replace(string _, string, string _)

            for (var i = 1; i < op.Arguments.Length; i++)
            {
                if (i == 2)
                    continue;

                if (!IsConstant(op.Arguments[i]))
                    return;
            }

            var properties = ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string?>(PatternIndexName, "1"),
                new KeyValuePair<string, string?>(RegexOptionsIndexName, op.Arguments.Length > 3 ? "3" : null),
                new KeyValuePair<string, string?>(RegexTimeoutIndexName, op.Arguments.Length > 4 ? "4" : null),
                new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 4 ? GetMilliseconds(op.Arguments[4].Value)?.ToString(CultureInfo.InvariantCulture) : null),
            });

            context.ReportDiagnostic(s_regexSourceGeneratorRule, properties, op);
        }
    }

    private static bool IsConstant(IArgumentOperation argumentOperation)
    {
        var valueOperation = argumentOperation.Value;
        if (valueOperation.ConstantValue.HasValue)
            return true;

        var compilation = argumentOperation.SemanticModel!.Compilation;
        if (valueOperation.Type.IsEqualTo(compilation.GetBestTypeByMetadataName("System.TimeSpan")))
        {
            return GetMilliseconds(valueOperation).HasValue;
        }

        return false;
    }

    private static int? GetMilliseconds(IOperation op)
    {
        return GetMilliseconds(op, 1d);

        static int? GetMilliseconds(IOperation op, double factor)
        {
            var compilation = op.SemanticModel!.Compilation;

            const double TicksToMilliseconds = 1d / TimeSpan.TicksPerMillisecond;
            const double SecondsToMilliseconds = 1000;
            const double MinutesToMilliseconds = 60 * 1000;
            const double HoursToMilliseconds = 60 * 60 * 1000;
            const double DaysToMilliseconds = 24 * 60 * 60 * 1000;

            op = op.UnwrapImplicitConversionOperations();
            if (op.ConstantValue.HasValue)
            {
                if (op.ConstantValue.HasValue && op.ConstantValue.Value is long int64Value)
                    return (int)(int64Value * factor);

                if (op.ConstantValue.HasValue && op.ConstantValue.Value is int int32Value)
                    return (int)(int32Value * factor);

                if (op.ConstantValue.HasValue && op.ConstantValue.Value is double doubleValue)
                    return (int)(doubleValue * factor);
            }

            if (op is IDefaultValueOperation)
                return 0;

            if (op is IInvocationOperation invocationOperation)
            {
                var method = invocationOperation.TargetMethod;
                if (method.IsStatic && method.ContainingType.IsEqualTo(compilation.GetBestTypeByMetadataName("System.TimeSpan")))
                {
                    return method.Name switch
                    {
                        "FromTicks" => GetMilliseconds(invocationOperation.Arguments[0].Value, TicksToMilliseconds),
                        "FromMilliseconds" => GetMilliseconds(invocationOperation.Arguments[0].Value, 1),
                        "FromSeconds" => GetMilliseconds(invocationOperation.Arguments[0].Value, SecondsToMilliseconds),
                        "FromMinutes" => GetMilliseconds(invocationOperation.Arguments[0].Value, MinutesToMilliseconds),
                        "FromHours" => GetMilliseconds(invocationOperation.Arguments[0].Value, HoursToMilliseconds),
                        "FromDays" => GetMilliseconds(invocationOperation.Arguments[0].Value, DaysToMilliseconds),
                        _ => null,
                    };
                }

                return null;
            }

            if (op is IFieldReferenceOperation fieldReferenceOperation)
            {
                var member = fieldReferenceOperation.Member;
                if (member.IsStatic && member.ContainingType.IsEqualTo(compilation.GetBestTypeByMetadataName("System.TimeSpan")))
                {
                    return member.Name switch
                    {
                        "Zero" => 0,
                        "MinValue" => (int)TimeSpan.MinValue.TotalMilliseconds,
                        "MaxValue" => (int)TimeSpan.MaxValue.TotalMilliseconds,
                        _ => null,
                    };
                }

                if (member.IsStatic && member.ContainingType.IsEqualTo(compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex")))
                {
                    return member.Name switch
                    {
                        "InfiniteMatchTimeout" => -1,
                        _ => null,
                    };
                }

                if (member.IsStatic && member.ContainingType.IsEqualTo(compilation.GetBestTypeByMetadataName("System.Threading.Timeout")))
                {
                    return member.Name switch
                    {
                        "InfiniteTimeSpan" => -1,
                        "Infinite" => -1,
                        _ => null,
                    };
                }

                return null;
            }

            if (op is IObjectCreationOperation objectCreationOperation)
            {
                if (objectCreationOperation.Type.IsEqualTo(compilation.GetBestTypeByMetadataName("System.TimeSpan")))
                {
                    switch (objectCreationOperation.Arguments.Length)
                    {
                        case 1: // new TimeSpan(long ticks)
                            return GetMilliseconds(objectCreationOperation.Arguments[0].Value, 1d / TimeSpan.TicksPerMillisecond);

                        case 3: // new TimeSpan(int hours, int minutes, int seconds)
                            return AddValues(
                                GetMilliseconds(objectCreationOperation.Arguments[0].Value, HoursToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[1].Value, MinutesToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[2].Value, SecondsToMilliseconds)
                                );

                        case 4: // new TimeSpan(int days, int hours, int minutes, int seconds)
                            return AddValues(
                                GetMilliseconds(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds)
                                );

                        case 5: // new TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
                            return AddValues(
                                GetMilliseconds(objectCreationOperation.Arguments[0].Value, DaysToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[1].Value, HoursToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[2].Value, MinutesToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[3].Value, SecondsToMilliseconds),
                                GetMilliseconds(objectCreationOperation.Arguments[4].Value, 1)
                                );
                    }

                    return null;
                }
            }

            return null;

            static int? AddValues(params int?[] values)
            {
                var result = 0;
                foreach (var value in values)
                {
                    if (!value.HasValue)
                        return null;

                    result += value.GetValueOrDefault();
                }

                return result;
            }
        }
    }
}
