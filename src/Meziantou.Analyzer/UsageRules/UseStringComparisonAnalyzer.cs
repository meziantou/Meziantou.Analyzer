using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringComparisonAnalyzer : DiagnosticAnalyzer
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

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var stringComparisonType = context.Compilation.GetTypeByMetadataName<StringComparison>();
            var stringType = context.Compilation.GetTypeByMetadataName<string>();

            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            if (!IsMethod(context, invocationExpr, stringType, nameof(string.Equals)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.IndexOf)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.IndexOfAny)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.LastIndexOf)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.LastIndexOfAny)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.EndsWith)) &&
                !IsMethod(context, invocationExpr, stringType, nameof(string.StartsWith)))
                return;

            if (!HasStringComparisonParameter(context, invocationExpr, stringComparisonType))
            {
                // Check if there is an overload with a StringComparison
                if (HasOverloadWithStringComparison(context, invocationExpr, stringComparisonType))
                {
                    var diagnostic = Diagnostic.Create(s_rule, invocationExpr.GetLocation(), GetMethodName(context, invocationExpr));
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool HasStringComparisonParameter(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax expressionSyntax, ITypeSymbol stringComparisonType)
        {
            foreach (var arg in expressionSyntax.ArgumentList.Arguments)
            {
                var argExpr = arg.Expression;
                if (context.SemanticModel.GetSymbolInfo(argExpr).Symbol?.ContainingType == stringComparisonType)
                    return true;
            }

            return false;
        }

        private static bool IsMethod(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax expression, ITypeSymbol type, string name)
        {
            var methodSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(expression).Symbol;
            if (methodSymbol == null)
                return false;

            if (methodSymbol.Name != name)
                return false;

            if (!type.Equals(methodSymbol.ContainingType))
                return false;

            return true;
        }

        private static string GetMethodName(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax expression)
        {

            var methodSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(expression).Symbol;
            if (methodSymbol == null)
                return null;

            return methodSymbol.Name;
        }

        private static bool HasOverloadWithStringComparison(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax expression, ITypeSymbol stringComparisonType)
        {
            var methodSymbol = (IMethodSymbol)context.SemanticModel.GetSymbolInfo(expression).Symbol;

            var members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
            return members.OfType<IMethodSymbol>().Any(IsOverload);

            bool IsOverload(IMethodSymbol member)
            {
                if (member.Equals(methodSymbol))
                    return false;

                // We look for methods that only have one more parameter of type StringComparison
                if (member.Parameters.Length - 1 != methodSymbol.Parameters.Length)
                    return false;

                var i = 0;
                var j = 0;
                while (i < methodSymbol.Parameters.Length && j < member.Parameters.Length)
                {
                    var x = methodSymbol.Parameters[i].Type;
                    var y = member.Parameters[j].Type;

                    if (stringComparisonType.Equals(y))
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
                if (i != j || (i == j && stringComparisonType.Equals(member.Parameters[j].Type)))
                    return true;

                return false;
            }
        }
    }
}
