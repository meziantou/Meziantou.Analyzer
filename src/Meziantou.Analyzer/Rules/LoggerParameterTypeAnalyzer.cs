﻿using System;
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
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggerParameterTypeAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.LoggerParameterType,
        title: "Log Parameter type is not valid",
        messageFormat: "Parameter '{0}' must be of type {1} but is of type '{2}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType));

    private static readonly DiagnosticDescriptor RuleSerilog = new(
        RuleIdentifiers.LoggerParameterType_Serilog,
        title: "Log Parameter type is not valid",
        messageFormat: "Parameter '{0}' must be of type {1} but is of type '{2}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_Serilog));

    private static readonly DiagnosticDescriptor RuleInvalid = new(
        RuleIdentifiers.LoggerParameterType_InvalidType,
        title: "The list of log parameter types contains an invalid type",
        messageFormat: "The type '{0}' does not match any symbol of the compilation",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_InvalidType));

    private static readonly DiagnosticDescriptor RuleDuplicate = new(
        RuleIdentifiers.LoggerParameterType_DuplicateRule,
        title: "The list of log parameter types contains a duplicate",
        messageFormat: "Parameter '{0}' is duplicated",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_DuplicateRule));

    private static readonly DiagnosticDescriptor RuleMissingConfiguration = new(
        RuleIdentifiers.LoggerParameterType_MissingConfiguration,
        title: "The log parameter has no configured type",
        messageFormat: "Log parameter '{0}' has no configured type",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.LoggerParameterType_MissingConfiguration));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleSerilog, RuleInvalid, RuleDuplicate, RuleMissingConfiguration);

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
        private static readonly HashSet<string> SerilogLogMethodNames = new(StringComparer.Ordinal) { "Debug", "Information", "Error", "Fatal", "Verbose", "Warning", "Write" };
        private static readonly char[] SerilogPrefixes = ['@', '$'];

        [SuppressMessage("MicrosoftCodeAnalysisPerformance", "RS1013:Start action has no registered non-end actions", Justification = "")]
        public AnalyzerContext(CompilationStartAnalysisContext context)
        {
            var compilation = context.Compilation;
            Configuration = LoggerConfigurationFile.Empty;

            LoggerSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
            SerilogILoggerSymbol = compilation.GetBestTypeByMetadataName("Serilog.ILogger");
            SerilogLogSymbol = compilation.GetBestTypeByMetadataName("Serilog.Log");
            if (LoggerSymbol is null && SerilogILoggerSymbol is null && SerilogLogSymbol is null)
                return;

            LoggerExtensionsSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
            LoggerMessageSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage");
            StructuredLogFieldAttributeSymbol = compilation.GetBestTypeByMetadataName("Meziantou.Analyzer.Annotations.StructuredLogFieldAttribute");

            SerilogLoggerEnrichmentConfigurationWithPropertySymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:Serilog.Configuration.LoggerEnrichmentConfiguration.WithProperty(System.String,System.Object,System.Boolean)", compilation);
            SerilogLogForContextSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:Serilog.Log.ForContext(System.String,System.Object,System.Boolean)", compilation);
            SerilogILoggerForContextSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:Serilog.ILogger.ForContext(System.String,System.Object,System.Boolean)", compilation);
            SerilogILoggerForContextLogEventLevelSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId("M:Serilog.LoggerExtensions.ForContext``1(Serilog.ILogger,Serilog.Events.LogEventLevel,System.String,``0,System.Boolean)", compilation);

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

                Dictionary<string, ITypeSymbol[]> configuration = new(StringComparer.Ordinal);
                foreach (var file in files)
                {
                    var sourceText = file.GetText(context.CancellationToken);
                    if (sourceText is null)
                        continue;

                    foreach (var line in sourceText.Lines)
                    {
                        if (line.Span.IsEmpty)
                            continue;

                        var lineText = line.ToString();
                        if (lineText.StartsWith('#'))
                            continue;

                        var parts = lineText.Split(';');
                        if (parts.Length == 1)
                            continue;

                        var types = new List<ITypeSymbol>(capacity: parts.Length - 1);
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
                                var symbols = DocumentationCommentId.GetSymbolsForReferenceId(typeName, compilation);
                                if (symbols.Length > 0)
                                {
                                    foreach (var symbol in symbols)
                                    {
                                        if (symbol.Kind is SymbolKind.NamedType or SymbolKind.ArrayType && symbol is ITypeSymbol typeSymbol)
                                        {
                                            types.Add(typeSymbol);
                                        }
                                        else
                                        {
                                            errors.Add(Diagnostic.Create(RuleInvalid, CreateLocation(file, sourceText, line), typeName));
                                        }
                                    }
                                }
                                else
                                {
                                    errors.Add(Diagnostic.Create(RuleInvalid, CreateLocation(file, sourceText, line), typeName));
                                }
                            }
                        }

                        if (types.Count > 0)
                        {
                            var keyName = parts[0];
                            if (configuration.ContainsKey(keyName))
                            {
                                errors.Add(Diagnostic.Create(RuleDuplicate, CreateLocation(file, sourceText, line), keyName));
                            }

                            configuration[keyName] = [.. types];
                        }
                    }
                }

                if (StructuredLogFieldAttributeSymbol is not null)
                {
                    var attributes = context.Compilation.Assembly.GetAttributes();
                    foreach (var attribute in attributes)
                    {
                        if (!attribute.AttributeClass.IsEqualTo(StructuredLogFieldAttributeSymbol))
                            continue;

                        if (attribute.ConstructorArguments is [{ Type.SpecialType: SpecialType.System_String, IsNull: false, Value: string name }, TypedConstant { Kind: TypedConstantKind.Array } types])
                        {
                            configuration[name] = types.Values.Select(v => v.Value as ITypeSymbol).WhereNotNull().ToArray();
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

        public INamedTypeSymbol? StructuredLogFieldAttributeSymbol { get; private set; }

        public INamedTypeSymbol? LoggerSymbol { get; }
        public INamedTypeSymbol? LoggerExtensionsSymbol { get; }
        public INamedTypeSymbol? LoggerMessageSymbol { get; }

        public INamedTypeSymbol? SerilogLogSymbol { get; }
        public INamedTypeSymbol? SerilogILoggerSymbol { get; }
        public ISymbol? SerilogLoggerEnrichmentConfigurationWithPropertySymbol { get; }
        public ISymbol? SerilogLogForContextSymbol { get; }
        public ISymbol? SerilogILoggerForContextSymbol { get; }
        public ISymbol? SerilogILoggerForContextLogEventLevelSymbol { get; }

        public LoggerConfigurationFile Configuration { get; }

        public bool IsValid => Configuration.Count > 0;

        public void AnalyzeInvocationDeclaration(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            var containingType = operation.TargetMethod.ContainingType;

            IOperation? formatExpression = null;
            (ITypeSymbol? Symbol, SyntaxNode Location)[]? argumentTypes = null;
            char[]? potentialNamePrefixes = null;

            if (FindLogParameters(operation.TargetMethod, out var messageParameter, out var argumentsParameter))
            {
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
                    argumentTypes = operation.TargetMethod.TypeArguments.Select((arg, index) => ((ITypeSymbol?)arg, GetSyntaxNode(operation, index))).ToArray();

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
                        if (parameter is null)
                            continue;

                        if (parameter.Equals(messageParameter, SymbolEqualityComparer.Default))
                        {
                            formatExpression = argument.Value;
                        }
                        else if (parameter.Equals(argumentsParameter, SymbolEqualityComparer.Default))
                        {
                            if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation arrayCreation && arrayCreation.Initializer is not null)
                            {
                                argumentTypes = arrayCreation.Initializer.ElementValues.Select(v => (v.UnwrapImplicitConversionOperations().Type, v.Syntax)).ToArray();
                            }
                        }
                    }
                }
            }
            else if (SerilogLogMethodNames.Contains(operation.TargetMethod.Name) && operation.TargetMethod.ContainingType.IsEqualToAny(SerilogLogSymbol, SerilogILoggerSymbol))
            {
                var templateIndex = FindIndexOfTemplate(operation.Arguments);
                if (templateIndex != -1)
                {
                    formatExpression = operation.Arguments[templateIndex].Value;
                    if (operation.Arguments.Length == templateIndex + 2)
                    {
                        var argument = operation.Arguments[templateIndex + 1];
                        potentialNamePrefixes = SerilogPrefixes;

                        if (argument.ArgumentKind == ArgumentKind.ParamArray && argument.Value is IArrayCreationOperation arrayCreation && arrayCreation.Initializer is not null)
                        {
                            argumentTypes = arrayCreation.Initializer.ElementValues.Select(v => (v.UnwrapImplicitConversionOperations().Type, v.Syntax)).ToArray();
                        }
                    }

                    if (operation.Arguments.Length >= templateIndex && argumentTypes is null)
                    {
                        argumentTypes = operation.Arguments.Skip(templateIndex + 1).Select(v => (v.Value.UnwrapImplicitConversionOperations().Type, v.Syntax)).ToArray();
                    }
                }

                static int FindIndexOfTemplate(ImmutableArray<IArgumentOperation> arguments)
                {
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        if (arguments[i].Parameter?.Name is "messageTemplate")
                            return i;
                    }

                    return -1;
                }
            }
            else if (operation.TargetMethod.IsEqualTo(SerilogLoggerEnrichmentConfigurationWithPropertySymbol) && operation.Arguments.Length >= 2)
            {
                if (operation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string valueName })
                {
                    var value = operation.Arguments[1].Value;
                    ValidateLogParameter(context, operation.Arguments[0].Value, SerilogPrefixes, valueName, (value.UnwrapImplicitConversionOperations().Type, value.Syntax));
                }

                return;
            }
            else if (operation.TargetMethod.IsEqualTo(SerilogLogForContextSymbol) && operation.Arguments.Length >= 2)
            {
                if (operation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string valueName })
                {
                    var value = operation.Arguments[1].Value;
                    ValidateLogParameter(context, operation.Arguments[0].Value, SerilogPrefixes, valueName, (value.UnwrapImplicitConversionOperations().Type, value.Syntax));
                }

                return;
            }
            else if (operation.TargetMethod.IsEqualTo(SerilogILoggerForContextSymbol) && operation.Arguments.Length >= 2)
            {
                if (operation.Arguments[0].Value.ConstantValue is { HasValue: true, Value: string valueName })
                {
                    var value = operation.Arguments[1].Value;
                    ValidateLogParameter(context, operation.Arguments[0].Value, SerilogPrefixes, valueName, (value.UnwrapImplicitConversionOperations().Type, value.Syntax));
                }

                return;
            }
            else if (operation.TargetMethod.OriginalDefinition.IsEqualTo(SerilogILoggerForContextLogEventLevelSymbol) && operation.Arguments.Length >= 3)
            {
                if (operation.Arguments[2].Value.ConstantValue is { HasValue: true, Value: string valueName })
                {
                    var value = operation.Arguments[3].Value;
                    var valueType = operation.TargetMethod.TypeArguments[0];
                    ValidateLogParameter(context, operation.Arguments[2].Value, SerilogPrefixes, valueName, (valueType, value.Syntax));
                }

                return;
            }

            if (formatExpression is not null && argumentTypes is not null)
            {
                var allowNonConstantFormat = context.Options.GetConfigurationValue(formatExpression, Rule.Id + ".allow_non_constant_formats", defaultValue: true);
                var format = TryGetFormatText(formatExpression, allowNonConstantFormat);
                if (format is null)
                    return;

                var logFormat = new LogValuesFormatter(format);
                for (var i = 0; i < logFormat.ValueNames.Count && i < argumentTypes.Length; i++)
                {
                    var name = logFormat.ValueNames[i];
                    var argumentType = argumentTypes[i];
                    ValidateLogParameter(context, formatExpression, potentialNamePrefixes, name, argumentType);
                }
            }
        }

        private void ValidateLogParameter(OperationAnalysisContext context, IOperation nameOperation, char[]? potentialNamePrefixes, string name, (ITypeSymbol? Symbol, SyntaxNode Location) argumentType)
        {
            name = RemovePrefix(name, potentialNamePrefixes);
            ValidateLogParameter(context, nameOperation, name, argumentType);
        }

        private void ValidateLogParameter(OperationAnalysisContext context, IOperation nameOperation, string name, (ITypeSymbol? Symbol, SyntaxNode Location) argumentType)
        {
            if (!Configuration.IsValid(context.Compilation, name, argumentType.Symbol, out var ruleFound))
            {
                var expectedSymbols = Configuration.GetSymbols(name);
                var expectedSymbolsStr = string.Join(" or ", expectedSymbols.Select(s => $"'{s.ToDisplayString()}'"));
                context.ReportDiagnostic(Rule, argumentType.Location, name, expectedSymbolsStr, argumentType.Symbol?.ToDisplayString());
            }

            if (!ruleFound)
            {
                context.ReportDiagnostic(RuleMissingConfiguration, nameOperation, name);
            }
        }

        private static string RemovePrefix(string name, char[]? potentialNamePrefixes)
        {
            if (potentialNamePrefixes is not null)
            {
                foreach (var prefix in potentialNamePrefixes)
                {
                    if (name.StartsWith(prefix))
                    {
                        name = name[1..];
                        break;
                    }
                }
            }

            return name;
        }

        private static string? TryGetFormatText(IOperation? argumentExpression, bool allowNonConstantFormat)
        {
            if (argumentExpression is null)
                return null;

            switch (argumentExpression)
            {
                case IOperation { ConstantValue: { HasValue: true, Value: string constantValue } }:
                    return constantValue;

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                    var leftText = TryGetFormatText(binary.LeftOperand, allowNonConstantFormat);
                    var rightText = TryGetFormatText(binary.RightOperand, allowNonConstantFormat);
                    return Concat(leftText, rightText, allowNonConstantFormat);

                case IInterpolatedStringOperation interpolatedString:
                    var result = "";
                    foreach (var part in interpolatedString.Parts)
                    {
                        result = Concat(result, TryGetFormatText(part, allowNonConstantFormat), allowNonConstantFormat);
                        if (result is null)
                            return null;
                    }

                    return result;

                case IInterpolatedStringTextOperation text:
                    return TryGetFormatText(text.Text, allowNonConstantFormat);

                default:
                    return null;
            }

            static string? Concat(string? first, string? second, bool allowNonConstantFormat)
            {
                if (!allowNonConstantFormat && (first is null || second is null))
                    return null;

                return first + second;
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

            return message is not null;
        }
    }

    private sealed class LoggerConfigurationFile(Dictionary<string, ITypeSymbol[]> configuration)
    {
        private static readonly SymbolEqualityComparer Comparer = GetComparer();

        private static SymbolEqualityComparer GetComparer()
        {
            var kindType = Type.GetType("Microsoft.CodeAnalysis.TypeCompareKind, Microsoft.CodeAnalysis", throwOnError: false);
            if (kindType != null)
            {
                // TypeCompareKind.
                var ctorParam1 = kindType.GetField("AllNullableIgnoreOptions")?.GetValue(null);
                var ctorParam2 = kindType.GetField("IgnoreTupleNames")?.GetValue(null);
                if (ctorParam1 is not null && ctorParam2 is not null)
                {
                    var ctor = typeof(SymbolEqualityComparer).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, binder: null, [kindType], modifiers: null);
                    if (ctor != null)
                    {
                        return (SymbolEqualityComparer)ctor.Invoke([(int)ctorParam1 | (int)ctorParam2]);
                    }
                }
            }

            return SymbolEqualityComparer.Default;
        }

        public static LoggerConfigurationFile Empty { get; } = new LoggerConfigurationFile(new(StringComparer.Ordinal));

        public int Count => configuration.Count;

        public bool IsValid(Compilation compilation, string name, ITypeSymbol? type, out bool hasRule)
        {
            if (configuration.TryGetValue(name, out var validSymbols))
            {
                hasRule = true;

                if (type is null)
                    return false;

                foreach (var validSymbol in validSymbols)
                {
                    if (Comparer.Equals(validSymbol, type))
                        return true;

                    var conversion = compilation.ClassifyConversion(type, validSymbol);
                    if (conversion.Exists && conversion.IsImplicit && conversion.IsNullable)
                        return true;
                }

                return false;
            }

            hasRule = false;
            return true;
        }

        public ISymbol[] GetSymbols(string name)
        {
            if (configuration.TryGetValue(name, out var symbols))
                return symbols;

            return [];
        }
    }

    // source: https://github.com/dotnet/roslyn-analyzers/blob/afa566573b7b1a2129d78a26f238a2ac3f8e58ef/src/NetAnalyzers/Core/Microsoft.NetCore.Analyzers/Runtime/LogValuesFormatter.cs
    private sealed class LogValuesFormatter
    {
        private static readonly char[] FormatDelimiters = [',', ':'];

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

        public List<string> ValueNames { get; } = [];

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
