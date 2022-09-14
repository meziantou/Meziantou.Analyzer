using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Meziantou.Analyzer.Internals;
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
            new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 2 ? TimeSpanOperation.GetMilliseconds(op.Arguments[2].Value)?.ToString(CultureInfo.InvariantCulture) : null),
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
                new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 3 ? TimeSpanOperation.GetMilliseconds(op.Arguments[3].Value)?.ToString(CultureInfo.InvariantCulture) : null),
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
                new KeyValuePair<string, string?>(RegexTimeoutName, op.Arguments.Length > 4 ? TimeSpanOperation.GetMilliseconds(op.Arguments[4].Value)?.ToString(CultureInfo.InvariantCulture) : null),
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
            return TimeSpanOperation.GetMilliseconds(valueOperation).HasValue;
        }

        return false;
    }
}
