using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

public abstract class RegexUsageAnalyzerBase : DiagnosticAnalyzer
{
    private static readonly string[] MethodNames = ["IsMatch", "Match", "Matches", "Replace", "Split"];

    private static readonly DiagnosticDescriptor TimeoutRule = new(
        RuleIdentifiers.MissingTimeoutParameterForRegex,
        title: "Add regex evaluation timeout",
        messageFormat: "Regular expressions should not be vulnerable to Denial of Service attacks",
        RuleCategories.Security,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingTimeoutParameterForRegex));

    private static readonly DiagnosticDescriptor ExplicitCaptureRule = new(
        RuleIdentifiers.UseRegexExplicitCaptureOptions,
        title: "Add RegexOptions.ExplicitCapture",
        messageFormat: "Add RegexOptions.ExplicitCapture to prevent capturing unneeded groups",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseRegexExplicitCaptureOptions));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TimeoutRule, ExplicitCaptureRule);

    private protected sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ITypeSymbol? _regexSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.Regex");
        private readonly ITypeSymbol? _regexOptionsSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.RegexOptions");
        private readonly ITypeSymbol? _generatedRegexAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Text.RegularExpressions.GeneratedRegexAttribute");
        private readonly ITypeSymbol? _timeSpanSymbol = compilation.GetBestTypeByMetadataName("System.TimeSpan");

        public void AnalyzeGeneratedRegexSymbol(SymbolAnalysisContext context)
        {
            if (context.Symbol is not (IPropertySymbol or IMethodSymbol { MethodKind: MethodKind.Ordinary }))
                return;

            if (_generatedRegexAttributeSymbol is null)
                return;

            var attributes = context.Symbol.GetAttributes(_generatedRegexAttributeSymbol, inherits: false);
            foreach (var attribute in attributes)
            {
                // Only analyze the symbol in the files that declared the attribute to avoid reporting multiple times for the same symbol
                var attributeSyntaxReference = attribute.ApplicationSyntaxReference;
                if (attributeSyntaxReference is not null && !context.Symbol.DeclaringSyntaxReferences.Any(reference => reference.SyntaxTree == attributeSyntaxReference.SyntaxTree && reference.Span.Contains(attributeSyntaxReference.Span)))
                    continue;

                var regexOptions = RegexOptions.None;

                // RegexOptions.ExplicitCapture
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var pattern = attribute.ConstructorArguments[0].Value as string;
                    regexOptions = (RegexOptions)(int)attribute.ConstructorArguments[1].Value!;
                    if (pattern is not null && ShouldAddExplicitCapture(pattern, regexOptions))
                    {
                        context.ReportDiagnostic(ExplicitCaptureRule, attribute);
                    }
                }

                // Timeout
                if (!HasNonBacktracking(regexOptions) && attribute.ConstructorArguments.Length < 3)
                {
                    context.ReportDiagnostic(TimeoutRule, attribute);
                }
            }
        }

        private static bool HasNonBacktracking(RegexOptions options) => ((int)options & 1024) == 1024;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (op is null || op.TargetMethod is null)
                return;

            if (!op.TargetMethod.IsStatic)
                return;

            if (!MethodNames.Contains(op.TargetMethod.Name, StringComparer.Ordinal))
                return;

            if (!op.TargetMethod.ContainingType.IsEqualTo(_regexSymbol))
                return;

            if (op.Arguments.Length == 0)
                return;

            var regexOptions = CheckRegexOptionsArgument(context, op.TargetMethod.IsStatic ? 1 : 0, op.Arguments, _regexOptionsSymbol);
            if (!HasNonBacktracking(regexOptions) && !CheckTimeout(op.Arguments))
            {
                context.ReportDiagnostic(TimeoutRule, op);
            }
        }

        public void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var op = (IObjectCreationOperation)context.Operation;
            if (op is null)
                return;

            if (op.Arguments.Length == 0)
                return;

            if (!op.Type.IsEqualTo(_regexSymbol))
                return;

            var regexOptions = CheckRegexOptionsArgument(context, 0, op.Arguments, _regexOptionsSymbol);
            if (!HasNonBacktracking(regexOptions) && !CheckTimeout(op.Arguments))
            {
                context.ReportDiagnostic(TimeoutRule, op);
            }
        }

        private bool CheckTimeout(ImmutableArray<IArgumentOperation> args)
        {
            if (_timeSpanSymbol is null)
                return false;

            return args.Last().Value.Type.IsEqualTo(_timeSpanSymbol);
        }

        private static RegexOptions CheckRegexOptionsArgument(OperationAnalysisContext context, int patternArgumentIndex, ImmutableArray<IArgumentOperation> arguments, ITypeSymbol? regexOptionsSymbol)
        {
            var pattern = GetPattern();
            var (regexOptions, regexOptionsArgument) = GetRegexOptions();
            if (pattern is not null && regexOptions is not null && regexOptionsArgument is not null)
            {
                if (ShouldAddExplicitCapture(pattern, regexOptions.Value))
                {
                    context.ReportDiagnostic(ExplicitCaptureRule, regexOptionsArgument);
                }
            }

            return regexOptions ?? RegexOptions.None;

            string? GetPattern()
            {
                if (patternArgumentIndex < arguments.Length)
                {
                    var argument = arguments[patternArgumentIndex];
                    if (argument.Value is not null && argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value is string pattern)
                        return pattern;
                }

                return null;
            }

            (RegexOptions?, IArgumentOperation?) GetRegexOptions()
            {
                if (regexOptionsSymbol is null)
                    return (null, null);

                var arg = arguments.FirstOrDefault(a => a.Parameter is not null && a.Parameter.Type.IsEqualTo(regexOptionsSymbol));
                if (arg is null || arg.Value is null || !arg.Value.ConstantValue.HasValue)
                    return (null, arg);

                return ((RegexOptions)arg.Value.ConstantValue.Value!, arg);
            }
        }

        private static bool ShouldAddExplicitCapture(string pattern, RegexOptions regexOptions)
        {
            if (!regexOptions.HasFlag(RegexOptions.ExplicitCapture) && !regexOptions.HasFlag(RegexOptions.ECMAScript)) // The 2 options are exclusives
            {
                // early exit
                if (!pattern.Contains('(', StringComparison.Ordinal))
                    return false;

                return HasUnnamedGroups(pattern, regexOptions);
            }

            return false;

            static bool HasUnnamedGroups(string pattern, RegexOptions options)
            {
                try
                {
                    options &= ~RegexOptions.Compiled; // Compiled options doesn't change anything but is much more resource consuming
                    var regex1 = new Regex(pattern, options, Regex.InfiniteMatchTimeout);
                    var regex2 = new Regex(pattern, options | RegexOptions.ExplicitCapture, Regex.InfiniteMatchTimeout);

                    // All groups are named => No need for explicit capture
                    if (regex1.GetGroupNames().Length == regex2.GetGroupNames().Length)
                        return false;
                }
                catch
                {
                }

                return true;
            }
        }
    }
}