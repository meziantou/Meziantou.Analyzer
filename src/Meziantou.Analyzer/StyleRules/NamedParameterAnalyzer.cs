using System.Collections.Immutable;
using System.Reflection;
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
                var taskGenericTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                var methodBaseTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.MethodBase");
                var fieldInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.FieldInfo");
                var propertyInfoTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Reflection.PropertyInfo");
                var assertTokenType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.Assert");
                var keyValuePairTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Collection.Generic.KeyValuePair`2");

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
                            var methodSymbol = (IMethodSymbol)symbolContext.SemanticModel.GetSymbolInfo(invocationExpression).Symbol;
                            var argumentIndex = ArgumentIndex(argument);

                            if (IsMethod(methodSymbol, taskTokenType, nameof(Task.ConfigureAwait)))
                                return;

                            if (IsMethod(methodSymbol, taskGenericTokenType, nameof(Task.ConfigureAwait)))
                                return;

                            if (IsMethod(methodSymbol, methodBaseTokenType, nameof(MethodBase.Invoke)) && argumentIndex == 0)
                                return;

                            if (IsMethod(methodSymbol, fieldInfoTokenType, nameof(FieldInfo.SetValue)) && argumentIndex == 0)
                                return;

                            if (IsMethod(methodSymbol, fieldInfoTokenType, nameof(FieldInfo.GetValue)) && argumentIndex == 0)
                                return;

                            if (IsMethod(methodSymbol, propertyInfoTokenType, nameof(PropertyInfo.SetValue)) && argumentIndex == 0)
                                return;

                            if (IsMethod(methodSymbol, propertyInfoTokenType, nameof(PropertyInfo.GetValue)) && argumentIndex == 0)
                                return;

                            if (IsMethod(methodSymbol, assertTokenType, "AreEqual"))
                                return;

                            if ((methodSymbol.Name == "Parse" || methodSymbol.Name == "TryParse") && argumentIndex == 0)
                                return;
                        }

                        symbolContext.ReportDiagnostic(Diagnostic.Create(s_rule, symbolContext.Node.GetLocation()));
                    }

                }, SyntaxKind.Argument);


            });
        }

        private static bool IsMethod(IMethodSymbol method, ITypeSymbol type, string name)
        {
            if (type == null || method == null)
                return false;
            
            if (method.Name != name)
                return false;
            
            if (!type.Equals(method.ContainingType.OriginalDefinition))
                return false;

            return true;
        }

        private static int ArgumentIndex(ArgumentSyntax argument)
        {
            var argumentListExpression = argument.FirstAncestorOrSelf<ArgumentListSyntax>();
            if (argumentListExpression == null)
                return -1;

            for (int i = 0; i < argumentListExpression.Arguments.Count; i++)
            {
                if (argumentListExpression.Arguments[i] == argument)
                    return i;
            }

            return -1;
        }
    }
}
