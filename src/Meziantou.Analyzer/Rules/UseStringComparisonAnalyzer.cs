using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
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

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var stringComparisonType = context.Compilation.GetTypeByMetadataName("System.StringComparison");
            var operation = (IInvocationOperation)context.Operation;

            if (stringComparisonType == null)
                return;

            if (!operation.HasArgumentOfType(stringComparisonType))
            {
                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (operation.IsInExpressionArgument())
                    return;

                // Check if there is an overload with a StringComparison
                if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(operation, stringComparisonType))
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

        private static bool IsNonCultureSensitiveMethod(IInvocationOperation operation)
        {
            var method = operation.TargetMethod;
            if (method == null)
                return false;

            // string.Equals(string)
            if (method.ContainingType.IsString() && method.Name == nameof(string.Equals) && method.Parameters.Length == 1 && method.Parameters[0].Type.IsString())
                return true;

            // string.Equals(string, string)
            if (method.ContainingType.IsString() && method.Name == nameof(string.Equals) && method.IsStatic && method.Parameters.Length == 2 && method.Parameters[0].Type.IsString() && method.Parameters[1].Type.IsString())
                return true;

            // string.IndexOf(char)
            if (method.ContainingType.IsString() && method.Name == nameof(string.IndexOf) && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                return true;

            // string.EndsWith(char)
            if (method.ContainingType.IsString() && method.Name == nameof(string.EndsWith) && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                return true;

            // string.StartsWith(char)
            if (method.ContainingType.IsString() && method.Name == nameof(string.StartsWith) && method.Parameters.Length == 1 && method.Parameters[0].Type.IsChar())
                return true;

            // JObject.Property / TryGetValue / GetValue
            var jobjectType = operation.SemanticModel!.Compilation.GetTypeByMetadataName("Newtonsoft.Json.Linq.JObject");
            if (method.ContainingType.IsEqualTo(jobjectType))
                return true;

            // Xunit.Assert.Contains/NotContains
            var xunitAssertType = operation.SemanticModel.Compilation.GetTypeByMetadataName("XUnit.Assert");
            if (method.ContainingType.IsEqualTo(xunitAssertType))
                return true;

            return false;
        }
    }
}
