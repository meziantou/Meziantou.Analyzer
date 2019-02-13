using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStringComparisonAnalyzer : DiagnosticAnalyzer
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

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var stringComparisonType = context.Compilation.GetTypeByMetadataName("System.StringComparison");
            var stringType = context.Compilation.GetSpecialType(SpecialType.System_String);

            var operation = (IInvocationOperation)context.Operation;
            if (!IsMethod(operation, stringType, nameof(string.Equals)) &&
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

            if (!HasArgumentOfType(operation, stringComparisonType))
            {
                // Check if there is an overload with a StringComparison
                if (HasOverloadWithAdditionalParameterOfType(operation, stringComparisonType))
                {
                    var diagnostic = Diagnostic.Create(s_rule, operation.Syntax.GetLocation(), operation.TargetMethod.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        internal static bool HasArgumentOfType(IInvocationOperation operation, ITypeSymbol stringComparisonType)
        {
            foreach (var arg in operation.Arguments)
            {
                if (stringComparisonType.Equals(arg.Value.Type.ContainingType))
                    return true;
            }

            return false;
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

        internal static bool HasOverloadWithAdditionalParameterOfType(IInvocationOperation operation, ITypeSymbol additionalParameterType)
        {
            var methodSymbol = operation.TargetMethod;
            var obsoleteAttribute = operation.SemanticModel.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

            var members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
            return members.OfType<IMethodSymbol>().Any(IsOverload);

            bool IsOverload(IMethodSymbol member)
            {
                if (member.Equals(methodSymbol))
                    return false;

                // We look for methods that only have one more parameter of type StringComparison
                if (member.Parameters.Length - 1 != methodSymbol.Parameters.Length)
                    return false;

                if (member.HasAttribute(obsoleteAttribute))
                    return false;

                var i = 0;
                var j = 0;
                while (i < methodSymbol.Parameters.Length && j < member.Parameters.Length)
                {
                    var x = methodSymbol.Parameters[i].Type;
                    var y = member.Parameters[j].Type;

                    if (additionalParameterType.Equals(y))
                    {
                        j++;
                        continue;
                    }

                    if (!x.Equals(y))
                        return false;

                    i++;
                    j++;
                }

                // Ensure the last argument is of type StringComparison
                return i != j || (i == j && additionalParameterType.Equals(member.Parameters[j].Type));
            }
        }

        internal static bool HasOverloadWithAdditionalParameterOfType(IInvocationOperation operation, params ITypeSymbol[] additionalParameterTypes)
        {
            var methodSymbol = operation.TargetMethod;
            var obsoleteAttribute = operation.SemanticModel.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

            var members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
            return members.OfType<IMethodSymbol>().Any(IsOverload);

            bool IsOverload(IMethodSymbol member)
            {
                if (member.Equals(methodSymbol))
                    return false;

                // We look for methods that only have one more parameter of type StringComparison
                if (member.Parameters.Length - additionalParameterTypes.Length != methodSymbol.Parameters.Length)
                    return false;

                if (member.HasAttribute(obsoleteAttribute))
                    return false;

                var additionalParameters = additionalParameterTypes.ToList();

                var i = 0;
                var j = 0;
                while (i < methodSymbol.Parameters.Length && j < member.Parameters.Length)
                {
                    var x = methodSymbol.Parameters[i].Type;
                    var y = member.Parameters[j].Type;

                    var indexOfParameter = additionalParameters.IndexOf(y);
                    if (indexOfParameter >= 0)
                    {
                        additionalParameters.RemoveAt(indexOfParameter);
                        j++;
                        continue;
                    }

                    if (!x.Equals(y))
                        return false;

                    i++;
                    j++;
                }

                // Ensure the last argument is of type StringComparison
                return i != j || (i == j && additionalParameterTypes.Equals(member.Parameters[j].Type));
            }
        }
    }
}
