using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegexUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] s_methodNames = { "IsMatch", "Match", "Matches", "Replace", "Split" };

    private static readonly DiagnosticDescriptor s_timeoutRule = new(
        RuleIdentifiers.MissingTimeoutParameterForRegex,
        title: "Add regex evaluation timeout",
        messageFormat: "Regular expressions should not be vulnerable to Denial of Service attacks",
        RuleCategories.Security,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingTimeoutParameterForRegex));

    private static readonly DiagnosticDescriptor s_explicitCaptureRule = new(
        RuleIdentifiers.UseRegexExplicitCaptureOptions,
        title: "Add RegexOptions.ExplicitCapture",
        messageFormat: "Add RegexOptions.ExplicitCapture to prevent capturing unneeded groups",
        RuleCategories.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseRegexExplicitCaptureOptions));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_timeoutRule, s_explicitCaptureRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind is not MethodKind.Ordinary)
            return;

        var generatorAttributeSymbol = context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.RegexGeneratorAttribute");
        if (generatorAttributeSymbol == null)
            return;

        foreach (var attribute in method.GetAttributes())
        {
            // https://github.com/dotnet/runtime/issues/58880
            if (attribute.AttributeClass.IsEqualTo(generatorAttributeSymbol))
            {
                // Timeout
                if (attribute.ConstructorArguments.Length < 3)
                {
                    if (attribute.ApplicationSyntaxReference != null)
                    {
                        context.ReportDiagnostic(s_timeoutRule, attribute.ApplicationSyntaxReference);
                    }
                    else
                    {
                        context.ReportDiagnostic(s_timeoutRule, method);
                    }
                }

                // RegexOptions.ExplicitCapture
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var pattern = attribute.ConstructorArguments[0].Value as string;
                    var regexOptions = (RegexOptions)(int)attribute.ConstructorArguments[1].Value!;
                    if (pattern != null && ShouldAddExplicitCapture(pattern, regexOptions))
                    {
                        if (attribute.ApplicationSyntaxReference != null)
                        {
                            context.ReportDiagnostic(s_explicitCaptureRule, attribute.ApplicationSyntaxReference);
                        }
                        else
                        {
                            context.ReportDiagnostic(s_explicitCaptureRule, method);
                        }
                    }
                }
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var op = (IInvocationOperation)context.Operation;
        if (op == null || op.TargetMethod == null)
            return;

        if (!op.TargetMethod.IsStatic)
            return;

        if (!s_methodNames.Contains(op.TargetMethod.Name, StringComparer.Ordinal))
            return;

        if (!op.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex")))
            return;

        if (op.Arguments.Length == 0)
            return;

        if (!CheckTimeout(context, op.Arguments))
        {
            context.ReportDiagnostic(s_timeoutRule, op);
        }

        CheckRegexOptionsArgument(context, op.TargetMethod.IsStatic ? 1 : 0, op.Arguments, context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.RegexOptions"));
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var op = (IObjectCreationOperation)context.Operation;
        if (op == null)
            return;

        if (op.Arguments.Length == 0)
            return;

        if (!op.Type.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex")))
            return;

        if (!CheckTimeout(context, op.Arguments))
        {
            context.ReportDiagnostic(s_timeoutRule, op);
        }

        CheckRegexOptionsArgument(context, 0, op.Arguments, context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.RegexOptions"));
    }

    private static bool CheckTimeout(OperationAnalysisContext context, ImmutableArray<IArgumentOperation> args)
    {
        return args.Last().Value.Type.IsEqualTo(context.Compilation.GetTypeByMetadataName("System.TimeSpan"));
    }

    private static void CheckRegexOptionsArgument(OperationAnalysisContext context, int patternArgumentIndex, ImmutableArray<IArgumentOperation> arguments, ITypeSymbol? regexOptionsSymbol)
    {

        var pattern = GetPattern();
        var (regexOptions, regexOptionsArgument) = GetRegexOptions();
        if (pattern != null && regexOptions != null && regexOptionsArgument != null)
        {
            if (ShouldAddExplicitCapture(pattern, regexOptions.Value))
            {
                context.ReportDiagnostic(s_explicitCaptureRule, regexOptionsArgument);
            }
        }

        string? GetPattern()
        {
            if (patternArgumentIndex < arguments.Length)
            {
                var argument = arguments[patternArgumentIndex];
                if (argument.Value != null && argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value is string pattern)
                    return pattern;
            }

            return null;
        }

        (RegexOptions?, IArgumentOperation?) GetRegexOptions()
        {
            if (regexOptionsSymbol == null)
                return (null, null);


            var arg = arguments.FirstOrDefault(a => a.Parameter != null && a.Parameter.Type.IsEqualTo(regexOptionsSymbol));
            if (arg == null || arg.Value == null || !arg.Value.ConstantValue.HasValue)
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
