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
        var formatArgumentIndex = -1;

        if (operation.Arguments.Length > 0)
        {
            var firstArg = operation.Arguments[0];
            if (firstArg.Parameter?.Type.IsEqualTo(formatProviderType) == true)
            {
                // First argument is IFormatProvider, so format string is the second argument
                if (operation.Arguments.Length > 1)
                {
                    formatArgument = operation.Arguments[1];
                    formatArgumentIndex = 1;
                }
            }
            else
            {
                // First argument is the format string
                formatArgument = firstArg;
                formatArgumentIndex = 0;
            }
        }

        if (formatArgument is null)
            return;

        // Check if there are any formatting arguments after the format string
        var hasFormattingArguments = false;
        for (var i = formatArgumentIndex + 1; i < operation.Arguments.Length; i++)
        {
            var arg = operation.Arguments[i];

            // Check if this is a params array argument
            if (arg.ArgumentKind == ArgumentKind.ParamArray && arg.Value is IArrayCreationOperation arrayCreation)
            {
                // Check if the array has any elements
                if (arrayCreation.Initializer is not null && arrayCreation.Initializer.ElementValues.Length > 0)
                {
                    hasFormattingArguments = true;
                    break;
                }
                // Skip empty params arrays
                continue;
            }

#if ROSLYN_4_14_OR_GREATER
            if (arg.ArgumentKind is ArgumentKind.ParamCollection && arg.Value is ICollectionExpressionOperation collectionExpression)
            {
                if (collectionExpression.Elements.Length > 0)
                {
                    hasFormattingArguments = true;
                    break;
                }

                continue;
            }
#endif

            // Skip other implicit arguments
            if (arg.IsImplicit)
                continue;

            hasFormattingArguments = true;
            break;
        }

        // Case 1: No formatting arguments at all - report regardless of format string
        if (!hasFormattingArguments)
        {
            context.ReportDiagnostic(Rule, operation);
            return;
        }

        // Case 2: Has formatting arguments but constant format string has no placeholders
        if (formatArgument.Value.ConstantValue.HasValue && formatArgument.Value.ConstantValue.Value is string formatString)
        {
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
            // Use IndexOf to find the next '{' - this can be vectorized for better performance
            var braceIndex = formatString.IndexOf('{', i);
            if (braceIndex == -1)
            {
                // No more opening braces
                return false;
            }

            i = braceIndex;

            // Check if it's an escaped brace
            if (i + 1 < formatString.Length && formatString[i + 1] == '{')
            {
                // Escaped opening brace, skip both
                i += 2;
                continue;
            }

            // Check if it looks like a placeholder
            // A valid placeholder is {digit...} where digit is 0-9 (ASCII digits only)
            var j = i + 1;
            var hasDigit = false;
            while (j < formatString.Length && formatString[j] is >= '0' and <= '9')
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

        return false;
    }
}
