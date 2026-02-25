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
        title: "The format string should use placeholders",
        messageFormat: "Use a string literal instead of a format method when the format string has no placeholders",
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
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var formatProviderType = compilationStartContext.Compilation.GetBestTypeByMetadataName("System.IFormatProvider");
            var consoleType = compilationStartContext.Compilation.GetBestTypeByMetadataName("System.Console");
            var stringBuilderType = compilationStartContext.Compilation.GetBestTypeByMetadataName("System.Text.StringBuilder");

            compilationStartContext.RegisterOperationAction(
                context => Analyze(context, formatProviderType, consoleType, stringBuilderType),
                OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ITypeSymbol? formatProviderType, ITypeSymbol? consoleType, ITypeSymbol? stringBuilderType)
    {
        var operation = (IInvocationOperation)context.Operation;
        var method = operation.TargetMethod;

        if (operation.Arguments.Length == 0)
            return;

        // Determine if this is a known format method, what the format arg index is,
        // and whether to report when there are no formatting arguments.
        int formatArgumentIndex;
        bool reportWhenNoFormattingArgs;

        if (method.ContainingType.SpecialType == SpecialType.System_String && method.Name == "Format")
        {
            // string.Format - report even when called with no args (e.g. string.Format("abc"))
            reportWhenNoFormattingArgs = true;
            formatArgumentIndex = GetFormatArgIndex(operation, formatProviderType);
        }
        else if (consoleType is not null && method.ContainingType.IsEqualTo(consoleType) &&
                 (method.Name == "Write" || method.Name == "WriteLine"))
        {
            // Console.Write / Console.WriteLine - only report when formatting arguments are supplied
            // (Console.Write("abc") is a valid non-format call)
            if (method.Parameters.Length <= 1)
                return;

            reportWhenNoFormattingArgs = false;
            formatArgumentIndex = 0;
        }
        else if (stringBuilderType is not null && method.ContainingType.IsEqualTo(stringBuilderType) &&
                 method.Name == "AppendFormat")
        {
            // StringBuilder.AppendFormat - report even when called with no args
            reportWhenNoFormattingArgs = true;
            formatArgumentIndex = GetFormatArgIndex(operation, formatProviderType);
        }
        else
        {
            return;
        }

        if (formatArgumentIndex < 0 || formatArgumentIndex >= operation.Arguments.Length)
            return;

        var formatArgument = operation.Arguments[formatArgumentIndex];

        // Check if there are any formatting arguments after the format string
        var hasFormattingArguments = HasFormattingArguments(operation, formatArgumentIndex);

        // Case 1: No formatting arguments at all
        if (!hasFormattingArguments)
        {
            if (reportWhenNoFormattingArgs)
            {
                context.ReportDiagnostic(Rule, operation);
            }

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

    private static int GetFormatArgIndex(IInvocationOperation operation, ITypeSymbol? formatProviderType)
    {
        if (operation.Arguments.Length > 0 && operation.Arguments[0].Parameter?.Type.IsEqualTo(formatProviderType) == true)
            return 1;

        return 0;
    }

    private static bool HasFormattingArguments(IInvocationOperation operation, int formatArgumentIndex)
    {
        for (var i = formatArgumentIndex + 1; i < operation.Arguments.Length; i++)
        {
            var arg = operation.Arguments[i];

            // Check if this is a params array argument
            if (arg.ArgumentKind == ArgumentKind.ParamArray && arg.Value is IArrayCreationOperation arrayCreation)
            {
                // Check if the array has any elements
                if (arrayCreation.Initializer is not null && arrayCreation.Initializer.ElementValues.Length > 0)
                    return true;

                // Skip empty params arrays
                continue;
            }

#if ROSLYN_4_14_OR_GREATER
            if (arg.ArgumentKind is ArgumentKind.ParamCollection && arg.Value is ICollectionExpressionOperation collectionExpression)
            {
                if (collectionExpression.Elements.Length > 0)
                    return true;

                continue;
            }
#endif

            // Skip arguments that were not explicitly provided (e.g. default values)
            if (arg.ArgumentKind is ArgumentKind.DefaultValue)
                continue;

            return true;
        }

        return false;
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
