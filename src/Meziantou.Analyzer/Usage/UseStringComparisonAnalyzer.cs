using System.Collections.Immutable;
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
            messageFormat: "StringComparison is missing",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            // invocationExpr.Expression is the expression before "(", here "string.Equals".
            // In this case it should be a MemberAccessExpressionSyntax, with a member name "Equals"
            var memberAccessExpr = invocationExpr.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpr == null)
                return;

            if (memberAccessExpr.Name.ToString() != nameof(string.Equals))
                return;

            // Now we need to get the semantic model of this node to get the type of the node
            // So, we can check it is of type string whatever the way you define it (string or System.String)
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol as IMethodSymbol;
            if (memberSymbol == null)
                return;

            // Check the method is a member of the class string
            if (memberSymbol.ContainingType.SpecialType != SpecialType.System_String)
                return;

            // If there are not 3 arguments, the comparison type is missing => report it
            // We could improve this validation by checking the types of the arguments, but it would be a little longer for this post.
            var argumentList = invocationExpr.ArgumentList;
            if ((argumentList?.Arguments.Count ?? 0) == 2)
            {
                var diagnostic = Diagnostic.Create(s_rule, invocationExpr.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
