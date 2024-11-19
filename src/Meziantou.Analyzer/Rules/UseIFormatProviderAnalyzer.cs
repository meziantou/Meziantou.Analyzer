using System.Collections.Generic;
using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseIFormatProviderAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseIFormatProviderParameter,
        title: "IFormatProvider is missing",
        messageFormat: "Use an overload of '{0}' that has a '{1}' parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseIFormatProviderParameter));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly CultureSensitiveFormattingContext _cultureSensitiveContext = new(compilation);
        private readonly OverloadFinder _overloadFinder = new(compilation);
        private readonly HashSet<ISymbol> _excludedMethods = CreateExcludedMethods(compilation);

        private static HashSet<ISymbol> CreateExcludedMethods(Compilation compilation)
        {
            var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            AddDocumentationId(result, compilation, "M:System.Convert.ToChar(System.String)");
            AddDocumentationId(result, compilation, "M:System.Convert.ToChar(System.Object)");
            AddDocumentationId(result, compilation, "M:System.Convert.ToBoolean(System.String)");
            AddDocumentationId(result, compilation, "M:System.Convert.ToBoolean(System.Object)");
            return result;

            static void AddDocumentationId(HashSet<ISymbol> result, Compilation compilation, string id)
            {
                foreach (var item in DocumentationCommentId.GetSymbolsForDeclarationId(id, compilation))
                {
                    result.Add(item);
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (operation is null)
                return;

            if (IsExcludedMethod(context, operation))
                return;

            var options = MustUnwrapNullableTypes(context, operation) ? CultureSensitiveOptions.UnwrapNullableOfT : CultureSensitiveOptions.None;
            if (!_cultureSensitiveContext.IsCultureSensitiveOperation(operation, options))
                return;

            if (_cultureSensitiveContext.FormatProviderSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.FormatProviderSymbol))
            {
                if (operation.TargetMethod.Name == "ToString" && operation.Arguments.Length == 0 && operation.TargetMethod.ContainingType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, operation, includeObsoleteMethods: false, allowOptionalParameters: false, _cultureSensitiveContext.FormatProviderSymbol);
                if (overload is not null)
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var targetMethodType = operation.TargetMethod.ContainingType;
                if (targetMethodType.IsNumberType() && _cultureSensitiveContext.NumberStyleSymbol is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(operation.TargetMethod, operation, _cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.NumberStyleSymbol))
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                    return;
                }

                var isDateTime = targetMethodType.IsDateTime() || targetMethodType.IsEqualToAny(_cultureSensitiveContext.DateTimeOffsetSymbol, _cultureSensitiveContext.DateOnlySymbol, _cultureSensitiveContext.TimeOnlySymbol);
                if (isDateTime)
                {
                    if (_cultureSensitiveContext.DateTimeStyleSymbol is not null && _overloadFinder.HasOverloadWithAdditionalParameterOfType(operation.TargetMethod, operation, _cultureSensitiveContext.FormatProviderSymbol, _cultureSensitiveContext.DateTimeStyleSymbol))
                    {
                        context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.FormatProviderSymbol.ToDisplayString());
                        return;
                    }
                }
            }

            if (_cultureSensitiveContext.CultureInfoSymbol is not null && !operation.HasArgumentOfType(_cultureSensitiveContext.CultureInfoSymbol))
            {
                var overload = _overloadFinder.FindOverloadWithAdditionalParameterOfType(operation.TargetMethod, includeObsoleteMethods: false, allowOptionalParameters: false, _cultureSensitiveContext.CultureInfoSymbol);
                if (overload is not null)
                {
                    context.ReportDiagnostic(Rule, operation, operation.TargetMethod.Name, _cultureSensitiveContext.CultureInfoSymbol.ToDisplayString());
                    return;
                }
            }
        }

        private bool IsExcludedMethod(OperationAnalysisContext context, IInvocationOperation operation)
        {
            if (_excludedMethods.Contains(operation.TargetMethod))
                return true;

            // ToString show culture-sensitive data by default
            if (operation?.GetContainingMethod(context.CancellationToken)?.Name == "ToString")
            {
                return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, "MA0011.exclude_tostring_methods", defaultValue: true);
            }

            return false;
        }

        private static bool MustUnwrapNullableTypes(OperationAnalysisContext context, IOperation operation)
        {
            return context.Options.GetConfigurationValue(operation.Syntax.SyntaxTree, "MA0011.consider_nullable_types", defaultValue: true);
        }
    }
}
