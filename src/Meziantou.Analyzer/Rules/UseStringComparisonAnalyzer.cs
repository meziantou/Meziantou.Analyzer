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
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseStringComparison,
            title: "StringComparison is missing",
            messageFormat: "Use an overload of '{0}' that has a StringComparison parameter",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseStringComparison));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var stringComparisonType = context.Compilation.GetTypeByMetadataName("System.StringComparison");
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);

            var operation = (IInvocationOperation)context.Operation;
            if (!IsMethod(operation, stringType, nameof(string.Compare)) &&
                !IsMethod(operation, stringType, nameof(string.Equals)) &&
                !IsMethod(operation, stringType, nameof(string.IndexOf)) &&
                !IsMethod(operation, stringType, nameof(string.IndexOfAny)) &&
                !IsMethod(operation, stringType, nameof(string.LastIndexOf)) &&
                !IsMethod(operation, stringType, nameof(string.LastIndexOfAny)) &&
                !IsMethod(operation, stringType, nameof(string.EndsWith)) &&
                !IsMethod(operation, stringType, nameof(string.Replace)) &&
                !IsMethod(operation, stringType, nameof(string.StartsWith)))
            {
                return;
            }

            if (!operation.HasArgumentOfType(stringComparisonType))
            {
                // EntityFramework Core doesn't support StringComparison and evaluates everything client side...
                // https://github.com/aspnet/EntityFrameworkCore/issues/1222
                if (operation.IsInQueryableExpressionArgument())
                    return;

                // Check if there is an overload with a StringComparison
                if (operation.TargetMethod.HasOverloadWithAdditionalParameterOfType(context.Compilation, stringComparisonType))
                {
                    context.ReportDiagnostic(s_rule, operation, operation.TargetMethod.Name);
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

            if (!type.Equals(methodSymbol.ContainingType))
                return false;

            return true;
        }
    }
}
