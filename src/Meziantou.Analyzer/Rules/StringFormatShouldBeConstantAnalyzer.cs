using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringFormatShouldBeConstantAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.StringFormatShouldBeConstant,
        title: "string.Format should use a format string with placeholders",
        messageFormat: "Use string literal instead of string.Format when the format string has no placeholders",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.StringFormatShouldBeConstant));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var operation = (IInvocationOperation)context.Operation;

        // Check if it's a string.Format call
        if (operation.TargetMethod.Name != "Format")
            return;

        if (operation.TargetMethod.ContainingType.SpecialType != SpecialType.System_String)
            return;

        if (operation.Arguments.Length == 0)
            return;

        var formatProviderType = context.Compilation.GetTypeByMetadataName("System.IFormatProvider");

        // Find the format string argument (either first or second parameter depending on overload)
        IArgumentOperation? formatArgument = null;

        if (operation.Arguments.Length > 0)
        {
            var firstArg = operation.Arguments[0];
            if (formatProviderType is not null && firstArg.Parameter?.Type.Equals(formatProviderType, SymbolEqualityComparer.Default) == true)
            {
                // First argument is IFormatProvider, so format string is the second argument
                if (operation.Arguments.Length > 1)
                {
                    formatArgument = operation.Arguments[1];
                }
            }
            else
            {
                // First argument is the format string
                formatArgument = firstArg;
            }
        }

        if (formatArgument is null)
            return;

        // We only analyze constant format strings
        if (formatArgument.Value.ConstantValue.HasValue && formatArgument.Value.ConstantValue.Value is string formatString)
        {
            // Check if there are any parameters passed (beyond the format string and optional IFormatProvider)
            var hasParameters = false;

            foreach (var arg in operation.Arguments)
            {
                if (arg == formatArgument)
                    continue;

                // Skip IFormatProvider parameter
                if (formatProviderType is not null && arg.Parameter?.Type.Equals(formatProviderType, SymbolEqualityComparer.Default) == true)
                    continue;

                hasParameters = true;
                break;
            }

            // Case 1: No parameters at all (e.g., string.Format("value without argument"))
            if (!hasParameters)
            {
                context.ReportDiagnostic(Rule, operation);
                return;
            }

            // Case 2: Has parameters but format string has no placeholders
            if (!HasPlaceholders(formatString))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }
    }

    private static bool HasPlaceholders(string formatString)
    {
        // Look for valid placeholders {0}, {1}, etc.
        // Need to handle escaped braces {{ and }}
        var i = 0;
        while (i < formatString.Length)
        {
            var c = formatString[i];
            if (c == '{')
            {
                // Check if it's an escaped brace
                if (i + 1 < formatString.Length && formatString[i + 1] == '{')
                {
                    // Escaped opening brace, skip both
                    i += 2;
                    continue;
                }

                // Check if it looks like a placeholder
                // A valid placeholder is {digit...}
                var j = i + 1;
                var hasDigit = false;
                while (j < formatString.Length && char.IsDigit(formatString[j]))
                {
                    hasDigit = true;
                    j++;
                }

                // After the digits, we can have optional alignment (,number) or format (:format)
                // For simplicity, just check if we found a digit and there's a closing brace eventually
                if (hasDigit)
                {
                    // Look for the closing brace (allowing for alignment and format specifiers)
                    while (j < formatString.Length)
                    {
                        if (formatString[j] == '}')
                        {
                            // Found a valid placeholder
                            return true;
                        }
                        if (formatString[j] == '{')
                        {
                            // Found another opening brace before closing, not a valid placeholder
                            break;
                        }
                        j++;
                    }
                }
                i++;
            }
            else if (c == '}')
            {
                // Check if it's an escaped brace
                if (i + 1 < formatString.Length && formatString[i + 1] == '}')
                {
                    // Escaped closing brace, skip both
                    i += 2;
                    continue;
                }
                i++;
            }
            else
            {
                i++;
            }
        }

        return false;
    }
}
