using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.UsageRules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.UseConfigureAwaitFalse,
            title: "Use .ConfigureAwait(false)",
            messageFormat: "Use ConfigureAwait(false) as the current SynchronizationContext is not needed",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseConfigureAwaitFalse));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
        }

        private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            // if expression is of type ConfiguredTaskAwaitable, do nothing
            // If ConfigureAwait(false) somewhere in a method, all following await calls should have ConfigureAwait(false)
            // Use ConfigureAwait(false) everywhere except if the parent class is a WPF, Winform, or ASP.NET class, or ASP.NET Core (because there is no SynchronizationContext)
            var node = (AwaitExpressionSyntax)context.Node;
            if (IsConfiguredTaskAwaitable(context, node))
                return;

            if (MustUseConfigureAwait(context, node))
            {
                context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Node.GetLocation()));
            }
        }

        private static bool MustUseConfigureAwait(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax node)
        {
            if (HasPreviousConfigureAwait(context, node))
                return true;

            var containingClass = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(containingClass);
                if (symbol != null)
                {
                    if (Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Threading.DispatcherObject")) || // WPF
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Forms.Control")) || // WinForms
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Web.UI.WebControls.WebControl")) || // ASP.NET (Webforms)
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase"))) // ASP.NET Core (as there is no SynchronizationContext, ConfigureAwait(false) is useless)
                        return false;
                }
            }

            return true;
        }

        private static bool HasPreviousConfigureAwait(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax node)
        {
            // Find all previous awaits with ConfiguredAwait(false)
            // Use context.SemanticModel.AnalyzeControlFlow to check if the current await is accessible from one of the previous await
            // https://joshvarty.com/2015/03/24/learn-roslyn-now-control-flow-analysis/
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method != null)
            {
                var otherAwaitExpressions = method.DescendantNodes(n => true).OfType<AwaitExpressionSyntax>();
                foreach (var a in otherAwaitExpressions)
                {
                    if (IsPreviousConfigureAwait(a))
                        return true;

                    bool IsPreviousConfigureAwait(AwaitExpressionSyntax otherAwaitExpression)
                    {
                        if (a == node)
                            return false;

                        if (a.GetLocation().SourceSpan.Start > node.GetLocation().SourceSpan.Start)
                            return false;

                        if (!IsConfiguredTaskAwaitable(context, a))
                            return false;

                        var nodeStatement = node.FirstAncestorOrSelf<StatementSyntax>();
                        var parentStatement = a.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
                        while (parentStatement != null && nodeStatement != parentStatement)
                        {
                            if (!IsEndPointReachable(context, parentStatement))
                                return false;

                            parentStatement = parentStatement.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsEndPointReachable(SyntaxNodeAnalysisContext context, StatementSyntax statementSyntax)
        {
            var result = context.SemanticModel.AnalyzeControlFlow(statementSyntax);
            if (!result.Succeeded)
                return false;

            if (!result.EndPointIsReachable)
                return false;

            return true;
        }

        private static bool IsConfiguredTaskAwaitable(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax awaitSyntax)
        {
            var awaitExpressionType = context.SemanticModel.GetTypeInfo(awaitSyntax.Expression).ConvertedType;
            if (awaitExpressionType == null)
                return false;

            var configuredTaskAwaitableType = context.Compilation.GetTypeByMetadataName<ConfiguredTaskAwaitable>();
            return configuredTaskAwaitableType != null && configuredTaskAwaitableType.Equals(awaitExpressionType);
        }

        private static bool Implements(INamedTypeSymbol classSymbol, ITypeSymbol type)
        {
            if (type == null)
                return false;

            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (type.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
