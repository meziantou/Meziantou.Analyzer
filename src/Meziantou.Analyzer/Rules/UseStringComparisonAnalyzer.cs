using System;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseStringComparisonAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_avoidCultureSensitiveMethodRule = new(
        RuleIdentifiers.AvoidCultureSensitiveMethod,
        title: "Avoid implicit culture-sensitive methods",
        messageFormat: "Use an overload of '{0}' that has a StringComparison parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AvoidCultureSensitiveMethod));

    private static readonly DiagnosticDescriptor s_useStringComparisonRule = new(
        RuleIdentifiers.UseStringComparison,
        title: "StringComparison is missing",
        messageFormat: "Use an overload of '{0}' that has a StringComparison parameter",
        RuleCategories.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparison));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_avoidCultureSensitiveMethodRule, s_useStringComparisonRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var analyzerContext = new AnalyzerContext(context.Compilation);
            if (!analyzerContext.IsValid)
                return;

            context.RegisterOperationAction(analyzerContext.AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly OverloadFinder _overloadFinder = new OverloadFinder(compilation);
        private readonly OperationUtilities _operationUtilities = new OperationUtilities(compilation);
        private readonly INamedTypeSymbol _stringComparisonSymbol = compilation.GetBestTypeByMetadataName("System.StringComparison")!;
        private readonly INamedTypeSymbol? _jobjectSymbol = compilation.GetBestTypeByMetadataName("Newtonsoft.Json.Linq.JObject");
        private readonly INamedTypeSymbol? _xunitAssertSymbol = compilation.GetBestTypeByMetadataName("XUnit.Assert");

        public bool IsValid => _stringComparisonSymbol != null;

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation)context.Operation;
            if (!operation.HasArgumentOfType(_stringComparisonSymbol))
            {
                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (_operationUtilities.IsInExpressionContext(operation))
                    return;

                // Check if there is an overload with a StringComparison
                if (_overloadFinder.HasOverloadWithAdditionalParameterOfType(operation.TargetMethod, operation, _stringComparisonSymbol))
                {
                    if (IsNonCultureSensitiveMethod(operation))
                    {
                        context.ReportDiagnostic(s_useStringComparisonRule, operation, operation.TargetMethod.Name);
                    }
                    else
                    {
                        context.ReportDiagnostic(s_avoidCultureSensitiveMethodRule, operation, operation.TargetMethod.Name);
                    }
                }
            }
        }

        private static bool IsMethod(IInvocationOperation operation, ITypeSymbol type, string name)
        {
            var methodSymbol = operation.TargetMethod;
            if (methodSymbol == null)
                return false;

            if (!string.Equals(methodSymbol.Name, name, StringComparison.Ordinal))
                return false;

            if (!type.IsEqualTo(methodSymbol.ContainingType))
                return false;

            return true;
        }

        private bool IsNonCultureSensitiveMethod(IInvocationOperation operation)
        {
            var method = operation.TargetMethod;
            if (method == null)
                return false;

            if (method.ContainingType.IsString())
            {
                return method is
                { Name: nameof(string.GetHashCode), IsStatic: false, Parameters: [] } or
                { Name: nameof(string.Equals), Parameters: [{ Type.SpecialType: SpecialType.System_String }] } or
                { Name: nameof(string.Equals), IsStatic: true, Parameters: [{ Type.SpecialType: SpecialType.System_String }, { Type.SpecialType: SpecialType.System_String }] } or
                { Name: nameof(string.IndexOf), Parameters: [{ Type.SpecialType: SpecialType.System_Char }] } or
                { Name: nameof(string.EndsWith), Parameters: [{ Type.SpecialType: SpecialType.System_Char }] } or
                { Name: nameof(string.StartsWith), Parameters: [{ Type.SpecialType: SpecialType.System_Char }] } or
                { Name: nameof(string.Contains), Parameters: [{ Type.SpecialType: SpecialType.System_Char or SpecialType.System_String }] } or
                { Name: nameof(string.Replace), Parameters: [{ Type.SpecialType: SpecialType.System_String }, { Type.SpecialType: SpecialType.System_String }] };
            }

            // JObject.Property / TryGetValue / GetValue
            if (method.ContainingType.IsEqualTo(_jobjectSymbol))
                return true;

            // Xunit.Assert.Contains/NotContains
            if (method.ContainingType.IsEqualTo(_xunitAssertSymbol))
                return true;

            return false;
        }
    }
}
