using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamedParameterAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.NameParameter,
            title: "Name parameter",
            messageFormat: "Name the parameter to improve the readability of the code",
            RuleCategories.Style,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var taskTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

                compilationContext.RegisterSyntaxNodeAction(symbolContext =>
                {
                    var argument = (ArgumentSyntax)symbolContext.Node;
                    if (argument.NameColon != null)
                        return;

                    var kind = argument.Expression.Kind();
                    if (kind == SyntaxKind.TrueLiteralExpression ||
                        kind == SyntaxKind.FalseLiteralExpression ||
                        kind == SyntaxKind.NullLiteralExpression)
                    {
                        // Exclude in some methods such as ConfigureAwait(false)
                        var invocationExpression = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression != null)
                        {
                            if (IsMethod(symbolContext, invocationExpression, taskTokenType, nameof(Task.ConfigureAwait)))
                                return;
                        }

                        symbolContext.ReportDiagnostic(Diagnostic.Create(s_rule, symbolContext.Node.GetLocation()));
                    }

                }, SyntaxKind.Argument);


            });
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
    }
}
