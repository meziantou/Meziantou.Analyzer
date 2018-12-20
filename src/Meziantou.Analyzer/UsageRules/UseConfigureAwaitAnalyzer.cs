using System.Collections.Immutable;
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
            description: "");

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
            var awaitExpressionType = context.SemanticModel.GetTypeInfo(node.Expression).ConvertedType;
            if (awaitExpressionType == null)
                return;

            var configuredTaskAwaitableType = context.Compilation.GetTypeByMetadataName<ConfiguredTaskAwaitable>();
            if (configuredTaskAwaitableType != null && configuredTaskAwaitableType.Equals(awaitExpressionType))
                return;

            var containingClass = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(containingClass);
                if (symbol != null)
                {
                    if (Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Threading.DispatcherObject")) || // WPF
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Forms.Control")) || // WinForms
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Web.UI.WebControls.WebControl")) || // ASP.NET (Webforms)
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase"))) // ASP.NET Core
                        return;
                }
            }

            // TODO find all previous awaits with ConfiguredAwait(false)
            // Use context.SemanticModel.AnalyzeControlFlow to check if the current await is accessible from one of the previous await
            // https://joshvarty.com/2015/03/24/learn-roslyn-now-control-flow-analysis/

            context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Node.GetLocation()));
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
