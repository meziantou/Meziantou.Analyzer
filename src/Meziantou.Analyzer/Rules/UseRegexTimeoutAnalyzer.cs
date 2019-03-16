using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseRegexTimeoutAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string[] s_methodNames = { "IsMatch", "Match", "Matches", "Replace", "Split" };

        private static readonly DiagnosticDescriptor s_timeoutRule = new DiagnosticDescriptor(
            RuleIdentifiers.MissingTimeoutParameterForRegex,
            title: "Add timeout parameter",
            messageFormat: "Regular expressions should not be vulnerable to Denial of Service attacks",
            RuleCategories.Security,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingTimeoutParameterForRegex));

        private static readonly DiagnosticDescriptor s_explicitCaptureRule = new DiagnosticDescriptor(
            RuleIdentifiers.UseRegexExplicitCaptureOptions,
            title: "Add RegexOptions.ExplicitCapture",
            messageFormat: "Add RegexOptions.ExplicitCapture",
            RuleCategories.Security,
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
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            if (op == null || op.TargetMethod == null)
                return;

            if (!op.TargetMethod.IsStatic)
                return;

            if (!s_methodNames.Contains(op.TargetMethod.Name, StringComparer.Ordinal))
                return;

            if (!op.TargetMethod.ContainingType.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex")))
                return;

            if (op.Arguments.Length == 0)
                return;

            var arg = op.Arguments.Last();
            if (arg.Value == null || !arg.Value.Type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.TimeSpan")))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_timeoutRule, op.Syntax.GetLocation()));
            }

            CheckRegexOptionsArgument(context, op.TargetMethod.IsStatic ? 1 : 0, op.Arguments, context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.RegexOptions"));
        }

        private void AnalyzeObjectCreation(OperationAnalysisContext context)
        {
            var op = (IObjectCreationOperation)context.Operation;
            if (op == null)
                return;

            if (op.Arguments.Length == 0)
                return;

            if (!op.Type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.Regex")))
                return;

            if (!op.Arguments.Last().Value.Type.IsEqualsTo(context.Compilation.GetTypeByMetadataName("System.TimeSpan")))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_timeoutRule, op.Syntax.GetLocation()));
            }

            CheckRegexOptionsArgument(context, 0, op.Arguments, context.Compilation.GetTypeByMetadataName("System.Text.RegularExpressions.RegexOptions"));
        }

        private void CheckRegexOptionsArgument(OperationAnalysisContext context, int patternArgumentIndex, ImmutableArray<IArgumentOperation> arguments, ITypeSymbol regexOptionsSymbol)
        {
            if (regexOptionsSymbol == null)
                return;

            var arg = arguments.FirstOrDefault(a => a.Parameter.Type.IsEqualsTo(regexOptionsSymbol));
            if (arg == null || arg.Value == null)
                return;

            if (arg.Value.ConstantValue.HasValue)
            {
                var value = ((RegexOptions)arg.Value.ConstantValue.Value);
                if (!value.HasFlag(RegexOptions.ExplicitCapture) && !value.HasFlag(RegexOptions.ECMAScript)) // The 2 options are exclusives
                {
                    if (HasUnnamedGroups(value))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(s_explicitCaptureRule, arg.Syntax.GetLocation()));
                    }
                }
            }

            bool HasUnnamedGroups(RegexOptions options)
            {
                if (patternArgumentIndex < arguments.Length)
                {
                    var argument = arguments[patternArgumentIndex];
                    if (argument.Value != null && argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value is string pattern)
                    {
                        try
                        {
                            var regex1 = new Regex(pattern, options, Regex.InfiniteMatchTimeout);
                            var regex2 = new Regex(pattern, options | RegexOptions.ExplicitCapture, Regex.InfiniteMatchTimeout);

                            // All groups are named => No need for explicit capture
                            if (regex1.GetGroupNames().Length == regex2.GetGroupNames().Length)
                                return false;
                        }
                        catch
                        {
                        }
                    }
                }

                return true;
            }
        }
    }
}
