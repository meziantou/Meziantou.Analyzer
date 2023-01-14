using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggerParameterTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.LoggerParameterType,
        title: "Log Parameter type is not valid",
        messageFormat: "Parameter '{0}' must be of type {1} but is of type '{2}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType));

    private static readonly DiagnosticDescriptor s_ruleInvalid = new(
        RuleIdentifiers.LoggerParameterType_InvalidType,
        title: "The list of log parameter types contains an invalid type",
        messageFormat: "The type '{0}' does not match any symbol of the compilation",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_InvalidType));

    private static readonly DiagnosticDescriptor s_ruleDuplicate = new(
        RuleIdentifiers.LoggerParameterType_DuplicateRule,
        title: "The list of log parameter types contains a duplicate",
        messageFormat: "Parameter '{0}' is duplicated",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_DuplicateRule));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule, s_ruleInvalid, s_ruleDuplicate);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var ctx = new AnalyzerContext(context);
            if (!ctx.IsValid)
                return;

            context.RegisterOperationAction(ctx.AnalyzeInvocationDeclaration, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        [SuppressMessage("MicrosoftCodeAnalysisPerformance", "RS1013:Start action has no registered non-end actions", Justification = "")]
        public AnalyzerContext(CompilationStartAnalysisContext context)
        {
            var compilation = context.Compilation;
            LoggerSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            LoggerExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
            LoggerMessageSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage");

            var errors = new List<Diagnostic>();
            Configuration = LoadConfiguration();

            if (errors.Count > 0)
            {
                context.RegisterCompilationEndAction(context =>
                {
                    foreach (var error in errors)
                    {
                        context.ReportDiagnostic(error);
                    }
                });
            }

            LoggerConfigurationFile LoadConfiguration()
            {
                var files = context.Options.AdditionalFiles
                    .Where(file => Path.GetFileName(file.Path) is { } fileName && fileName.StartsWith("LoggerParameterTypes.", StringComparison.Ordinal) && fileName.EndsWith(".txt", StringComparison.Ordinal))
                    .OrderBy(file => file.Path, StringComparer.Ordinal);

                Dictionary<string, ISymbol[]> configuration = new(StringComparer.Ordinal);
                foreach (var file in files)
                {
                    var sourceText = file.GetText(context.CancellationToken);
                    if (sourceText == null)
                        continue;

                    foreach (var line in sourceText.Lines)
                    {
                        if (line.Span.IsEmpty)
                            continue;

                        var lineText = line.ToString();
                        if (lineText.StartsWith("#", StringComparison.Ordinal))
                            continue;

                        var parts = lineText.Split(';');
                        if (parts.Length == 1)
                            continue;

                        var types = new List<ISymbol>(capacity: parts.Length - 1);
                        for (var i = 1; i < parts.Length; i++)
                        {
                            var typeName = parts[i].Trim();
                            var type = compilation.GetTypesByMetadataName(typeName);
                            if (type.Length > 0)
                            {
                                types.AddRange(type);
                            }
                            else
                            {
                                var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(typeName, compilation);
                                if (symbols.Length > 0)
                                {
                                    foreach (var symbol in symbols)
                                    {
                                        if (symbol.Kind == SymbolKind.NamedType)
                                        {
                                            types.Add(symbol);
                                        }
                                        else
                                        {
                                            errors.Add(Diagnostic.Create(s_ruleInvalid, CreateLocation(file, sourceText, line), typeName));
                                        }
                                    }
                                }
                                else
                                {
                                    errors.Add(Diagnostic.Create(s_ruleInvalid, CreateLocation(file, sourceText, line), typeName));
                                }
                            }
                        }

                        if (types.Count > 0)
                        {
                            var keyName = parts[0];
                            if (configuration.ContainsKey(keyName))
                            {
                                errors.Add(Diagnostic.Create(s_ruleDuplicate, CreateLocation(file, sourceText, line), keyName));

                            }

                            configuration[keyName] = types.ToArray();
                        }
                    }
                }

                return new LoggerConfigurationFile(configuration);

                static Location CreateLocation(AdditionalText file, SourceText sourceText, TextLine line)
                {
                    return Location.Create(file.Path, line.Span, sourceText.Lines.GetLinePositionSpan(line.Span));
                }
            }
        }

        public INamedTypeSymbol? LoggerSymbol { get; }
        public INamedTypeSymbol? LoggerExtensionsSymbol { get; }
        public INamedTypeSymbol? LoggerMessageSymbol { get; }
        public LoggerConfigurationFile Configuration { get; }

        public bool IsValid => LoggerSymbol != null && LoggerExtensionsSymbol != null && LoggerMessageSymbol != null && Configuration.Count > 0;

        public void AnalyzeInvocationDeclaration(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var containingType = operation.TargetMethod.ContainingType;

            if (!FindLogParameters(operation.TargetMethod, out var messageParameter, out var argumentsParameter))
                return;

            IOperation? formatExpression = null;
            (ITypeSymbol Symbol, SyntaxNode Location)[]? argumentTypes = null;

            if (containingType.IsEqualTo(LoggerMessageSymbol))
            {
                // For LoggerMessage.Define, count type parameters on the invocation instead of arguments
                var arg = operation.Arguments.FirstOrDefault(argument =>
                {
                    var parameter = argument.Parameter;
                    if (parameter is null)
                        return false;

                    return parameter.Equals(messageParameter, SymbolEqualityComparer.Default);
                });

                if (arg is null)
                    return;

                formatExpression = arg.Value;
                argumentTypes = operation.TargetMethod.TypeArguments.Select((arg, index) => (arg, GetSyntaxNode(operation, index))).ToArray();

                static SyntaxNode GetSyntaxNode(IOperation operation, int index)
                {
                    if (operation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax { TypeArgumentList: not null and var args } } })
                    {
                        if (index < args.Arguments.Count)
                            return args.Arguments[index];
                    }

                    return operation.Syntax;
                }
            }
            else if (operation.TargetMethod.ContainingType.IsEqualTo(LoggerExtensionsSymbol))
            {
                foreach (var argument in operation.Arguments)
                {
                    var parameter = argument.Parameter;
                    if (parameter == null)
                        continue;

                    if (parameter.Equals(messageParameter, SymbolEqualityComparer.Default))
                    {
                        formatExpression = argument.Value;
                    }
                    else if (parameter.Equals(argumentsParameter, SymbolEqualityComparer.Default))
                    {
                        var parameterType = parameter.Type;
                        if (parameterType == null)
                            return;

                        if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation arrayCreation && arrayCreation.Initializer != null)
                        {
                            argumentTypes = arrayCreation.Initializer.ElementValues.Select(v => (v.GetActualType()!, v.Syntax)).ToArray();
                        }
                    }
                }
            }

            if (formatExpression is not null && argumentTypes is not null)
            {
                var format = TryGetFormatText(formatExpression);
                if (format == null)
                    return;

                var logFormat = new LogValuesFormatter(format);
                for (var i = 0; i < logFormat.ValueNames.Count && i < argumentTypes.Length; i++)
                {
                    var name = logFormat.ValueNames[i];
                    var argumentType = argumentTypes[i];

                    if (!Configuration.IsValid(name, argumentType.Symbol))
                    {
                        var expectedSymbols = Configuration.GetSymbols(name);
                        var expectedSymbolsStr = string.Join(" or ", expectedSymbols.Select(s => $"'{s.ToDisplayString()}'"));
                        context.ReportDiagnostic(s_rule, argumentType.Location, name, expectedSymbolsStr, argumentType.Symbol.ToDisplayString());
                    }
                }
            }
        }

        private string? TryGetFormatText(IOperation? argumentExpression)
        {
            if (argumentExpression is null)
                return null;

            switch (argumentExpression)
            {
                case IOperation { ConstantValue: { HasValue: true, Value: string constantValue } }:
                    return constantValue;

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                    var leftText = TryGetFormatText(binary.LeftOperand);
                    var rightText = TryGetFormatText(binary.RightOperand);

                    if (leftText != null && rightText != null)
                    {
                        return leftText + rightText;
                    }

                    return null;

                default:
                    return null;
            }
        }

        private static bool FindLogParameters(IMethodSymbol methodSymbol, [NotNullWhen(true)] out IParameterSymbol? message, out IParameterSymbol? arguments)
        {
            message = null;
            arguments = null;
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.IsString() &&
                    (string.Equals(parameter.Name, "message", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "messageFormat", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "formatString", StringComparison.Ordinal)))
                {
                    message = parameter;
                }
                // When calling logger.BeginScope("{Param}") generic overload would be selected
                else if (parameter.Type.SpecialType == SpecialType.System_String &&
                    methodSymbol.Name is "BeginScope" &&
                    parameter.Name is "state")
                {
                    message = parameter;
                }
                else if (parameter.IsParams &&
                    parameter.Name is "args")
                {
                    arguments = parameter;
                }
            }

            return message != null;
        }
    }

    private sealed class LoggerConfigurationFile
    {
        private readonly Dictionary<string, ISymbol[]> _configuration;

        public LoggerConfigurationFile(Dictionary<string, ISymbol[]> configuration)
        {
            _configuration = configuration;
        }

        public int Count => _configuration.Count;

        public bool IsValid(string name, ISymbol type)
        {
            if (_configuration.TryGetValue(name, out var validSymbols))
            {
                foreach (var validSymbol in validSymbols)
                {
                    if (validSymbol.IsEqualTo(type))
                        return true;
                }

                return false;
            }

            return true;
        }

        public ISymbol[] GetSymbols(string name)
        {
            if (_configuration.TryGetValue(name, out var symbols))
                return symbols;

            return Array.Empty<ISymbol>();
        }
    }

    // source: https://github.com/dotnet/roslyn-analyzers/blob/afa566573b7b1a2129d78a26f238a2ac3f8e58ef/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/Runtime/LogValuesFormatter.cs
    private sealed class LogValuesFormatter
    {
        private static readonly char[] FormatDelimiters = { ',', ':' };

        public LogValuesFormatter(string format)
        {
            var sb = new StringBuilder();
            var scanIndex = 0;
            var endIndex = format.Length;

            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);

                if (closeBraceIndex == endIndex)
                {
                    sb.Append(format, scanIndex, endIndex - scanIndex);
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax : { index[,alignment][ :formatString] }.
                    var formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);

                    sb.Append(format, scanIndex, openBraceIndex - scanIndex + 1);
                    sb.Append(ValueNames.Count.ToString(CultureInfo.InvariantCulture));
                    ValueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                    sb.Append(format, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

                    scanIndex = closeBraceIndex + 1;
                }
            }
        }

        public List<string> ValueNames { get; } = new List<string>();

        private static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
        {
            // Example: {{prefix{{{Argument}}}suffix}}.
            var braceIndex = endIndex;
            var scanIndex = startIndex;
            var braceOccurrenceCount = 0;

            while (scanIndex < endIndex)
            {
                if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
                {
                    if (braceOccurrenceCount % 2 == 0)
                    {
                        // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                        braceOccurrenceCount = 0;
                        braceIndex = endIndex;
                    }
                    else
                    {
                        // An unescaped '{' or '}' found.
                        break;
                    }
                }
                else if (format[scanIndex] == brace)
                {
                    if (brace == '}')
                    {
                        if (braceOccurrenceCount == 0)
                        {
                            // For '}' pick the first occurrence.
                            braceIndex = scanIndex;
                        }
                    }
                    else
                    {
                        // For '{' pick the last occurrence.
                        braceIndex = scanIndex;
                    }

                    braceOccurrenceCount++;
                }

                scanIndex++;
            }

            return braceIndex;
        }

        private static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
        {
            var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
            return findIndex == -1 ? endIndex : findIndex;
        }
    }
}
