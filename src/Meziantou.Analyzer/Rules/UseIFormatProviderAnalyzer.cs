using System;
using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.UseIFormatProviderParameter,
        title: "IFormatProvider is missing",
        messageFormat: "Use an overload of '{0}' that has a '{1}' parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext
    {
        public AnalyzerContext(Compilation compilation)
        {
            FormatProviderSymbol = compilation.GetBestTypeByMetadataName("System.IFormatProvider");
            CultureInfoSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.CultureInfo");
            NumberStyleSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.NumberStyles");
            DateTimeStyleSymbol = compilation.GetBestTypeByMetadataName("System.Globalization.DateTimeStyles");
            StringBuilderSymbol = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder");
            StringBuilder_AppendInterpolatedStringHandlerSymbol = compilation.GetBestTypeByMetadataName("System.Text.StringBuilder+AppendInterpolatedStringHandler");
            GuidSymbol = compilation.GetBestTypeByMetadataName("System.Guid");
            EnumSymbol = compilation.GetBestTypeByMetadataName("System.Enum");
            DateTimeOffsetSymbol = compilation.GetBestTypeByMetadataName("System.DateTimeOffset");
        }

        public INamedTypeSymbol? FormatProviderSymbol { get; }
        public INamedTypeSymbol? CultureInfoSymbol { get; }
        public INamedTypeSymbol? NumberStyleSymbol { get; }
        public INamedTypeSymbol? DateTimeStyleSymbol { get; }
        public INamedTypeSymbol? StringBuilderSymbol { get; }
        public INamedTypeSymbol? StringBuilder_AppendInterpolatedStringHandlerSymbol { get; }
        public INamedTypeSymbol? GuidSymbol { get; }
        public INamedTypeSymbol? EnumSymbol { get; }
        public INamedTypeSymbol? DateTimeOffsetSymbol { get; }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation == null)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            if (!IsCultureSensitiveOperation(operation))
                return;

            if (FormatProviderSymbol != null && !operation.HasArgumentOfType(FormatProviderSymbol))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(operation, includeObsoleteMethods: false, FormatProviderSymbol);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, FormatProviderSymbol.ToDisplayString());
                    return;
                }

                if (operation.TargetMethod.ContainingType.IsNumberType() && operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, FormatProviderSymbol, NumberStyleSymbol))
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var isDateTime = operation.TargetMethod.ContainingType.IsDateTime() || operation.TargetMethod.ContainingType.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("System.DateTimeOffset"));
                if (isDateTime)
                {
                    if (operation.Arguments.Length >= 1 && IsInvariantDateTimeFormat(operation.Arguments[0].Value))
                        return;

                    if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, FormatProviderSymbol, DateTimeStyleSymbol))
                    {
                        context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, FormatProviderSymbol.ToDisplayString());
                        return;
                    }
                }
            }

            if (CultureInfoSymbol != null && !operation.HasArgumentOfType(CultureInfoSymbol))
            {
                var overload = operation.TargetMethod.FindOverloadWithAdditionalParameterOfType(context.Compilation, includeObsoleteMethods: false, CultureInfoSymbol);
                if (overload != null)
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name, CultureInfoSymbol.ToDisplayString());
                    return;
                }
            }
        }

        private static bool IsInvariantDateTimeFormat(IOperation? valueOperation)
        {
            return valueOperation is { ConstantValue: { HasValue: true, Value: "o" or "O" or "r" or "R" or "s" or "u" } };
        }

        private static bool IsExcludedMethod(OperationAnalysisContext context, IOperation operation)
        {
            // ToString show culture-sensitive data by default
            if (operation?.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, "MA0011.exclude_tostring_methods", defaultValue: true);
            }

            return false;
        }

        private bool IsCultureSensitiveOperation(IOperation operation)
        {
            if (operation is IInvocationOperation invocation)
            {
                var methodName = invocation.TargetMethod.Name;
                if (methodName is "ToString")
                {
                    // Boolean.ToString(IFormatProvider) should not be used
                    if (invocation.TargetMethod.ContainingType.IsBoolean())
                        return false;

                    // Char.ToString(IFormatProvider) should not be used
                    if (invocation.TargetMethod.ContainingType.IsChar())
                        return false;

                    // Guid.ToString(IFormatProvider) should not be used
                    if (invocation.TargetMethod.ContainingType.IsEqualTo(GuidSymbol))
                        return false;

                    // Enum.ToString(IFormatProvider) should not be used
                    if (invocation.TargetMethod.ContainingType.IsEqualTo(EnumSymbol))
                        return false;

                    // DateTime.ToString() or DateTimeOffset.ToString() with invariant formats (o, O, r, R, s, u)
                    if (invocation.Arguments.Length == 1 && (invocation.TargetMethod.ContainingType.IsDateTime() || invocation.TargetMethod.ContainingType.IsEqualTo(DateTimeOffsetSymbol)))
                    {
                        if (IsInvariantDateTimeFormat(invocation.Arguments[0].Value))
                            return false;
                    }
                }
                else if (methodName is "Parse" or "TryParse")
                {
                    // Guid.Parse / Guid.TryParse are culture insensitive
                    if (invocation.TargetMethod.ContainingType.IsEqualTo(GuidSymbol))
                        return false;

                    // Char.Parse / Char.TryParse are culture insensitive
                    if (invocation.TargetMethod.ContainingType.IsChar())
                        return false;
                }
                else if (methodName is "Append" or "AppendLine" && invocation.TargetMethod.ContainingType.IsEqualTo(StringBuilderSymbol))
                {
                    // stringBuilder.AppendLine($"foo{bar}") when bar is a string
                    if (invocation.Arguments.Length == 1 && invocation.Arguments[0].Value.Type.IsEqualTo(StringBuilder_AppendInterpolatedStringHandlerSymbol) && !IsCultureSensitiveOperation(invocation.Arguments[0].Value))
                        return false;
                }
            }

#if CSHARP10_OR_GREATER
            if (operation is IInterpolatedStringHandlerCreationOperation handler)
                return IsCultureSensitiveOperation(handler.Content);

            if (operation is IInterpolatedStringAdditionOperation interpolatedStringAddition)
                return IsCultureSensitiveOperation(interpolatedStringAddition.Left) || IsCultureSensitiveOperation(interpolatedStringAddition.Right);
#endif

            if (operation is IInterpolatedStringOperation interpolatedString)
            {
                if (interpolatedString.Parts.Length == 0)
                    return false;

                foreach (var part in interpolatedString.Parts)
                {
                    if (part is IInterpolatedStringTextOperation)
                        continue;

                    if (part is IInterpolationOperation content)
                    {
                        if (content.Expression.Type.IsDateTime() || content.Expression.Type.IsEqualTo(DateTimeOffsetSymbol))
                        {
                            if (!IsInvariantDateTimeFormat(content.FormatString))
                                return true;
                        }
                        else if (IsCultureSensitiveType(content.Expression.GetActualType()))
                        {
                            return true;
                        }
                    }
#if CSHARP10_OR_GREATER
                    else if (part is IInterpolatedStringAppendOperation append)
                    {
                        if (append.AppendCall is IInvocationOperation appendInvocation)
                        {
                            if (appendInvocation.Arguments.Length == 1)
                            {
                                if (IsCultureSensitiveType(appendInvocation.Arguments[0].Value.Type))
                                    return true;
                            }
                            else if (appendInvocation.Arguments.Length == 2)
                            {
                                var expression = appendInvocation.Arguments[0].Value;
                                if (expression.Type.IsDateTime() || expression.Type.IsEqualTo(DateTimeOffsetSymbol))
                                {
                                    if (!IsInvariantDateTimeFormat(appendInvocation.Arguments[1].Value))
                                        return true;
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
#endif
                    else
                    {
                        return true;
                    }
                }

                return false;
            }

            if (operation is ILocalReferenceOperation localReference)
                return IsCultureSensitiveType(localReference.Type);

            if (operation is IParameterReferenceOperation parameterReference)
                return IsCultureSensitiveType(parameterReference.Type);

            if (operation is IMemberReferenceOperation memberReference)
                return IsCultureSensitiveType(memberReference.Type);

            if (operation is ILiteralOperation literal)
                return IsCultureSensitiveType(literal.Type);

            // Unknown operation
            return true;
        }

        private bool IsCultureSensitiveType(ITypeSymbol? symbol)
        {
            if (symbol == null)
                return true;

            if (symbol.SpecialType is SpecialType.System_Boolean or SpecialType.System_String or SpecialType.System_Char)
                return false;

            if (symbol.IsEqualTo(GuidSymbol))
                return false;

            return true;
        }
    }
}
