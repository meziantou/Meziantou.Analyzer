using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
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
            if (!CanAddConfigureAwait(context, node))
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
                    if (symbol.InheritsFrom(context.Compilation.GetTypeByMetadataName("System.Windows.Threading.DispatcherObject")) || // WPF
                        symbol.Implements(context.Compilation.GetTypeByMetadataName("System.Windows.Input.ICommand")) || // WPF
                        symbol.InheritsFrom(context.Compilation.GetTypeByMetadataName("System.Windows.Forms.Control")) || // WinForms
                        symbol.InheritsFrom(context.Compilation.GetTypeByMetadataName("System.Web.UI.WebControls.WebControl")) || // ASP.NET (Webforms)
                        symbol.InheritsFrom(context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase")) || // ASP.NET Core (as there is no SynchronizationContext, ConfigureAwait(false) is useless)
                        symbol.Implements(context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper")) || // ASP.NET Core
                        symbol.Implements(context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent")) || // ASP.NET Core
                        symbol.Implements(context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata")))  // ASP.NET Core
                    {
                        return false;
                    }
                }
            }

            var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null)
            {
                var methodInfo = context.SemanticModel.GetDeclaredSymbol(containingMethod) as IMethodSymbol;
                if (methodInfo != null)
                {
                    if (methodInfo.HasAttribute(context.Compilation.GetTypeByMetadataName("Xunit.FactAttribute")) ||
                        methodInfo.HasAttribute(context.Compilation.GetTypeByMetadataName("NUnit.Framework.NUnitAttribute")) ||
                        methodInfo.HasAttribute(context.Compilation.GetTypeByMetadataName("NUnit.Framework.TheoryAttribute")) ||
                        methodInfo.HasAttribute(context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")) ||
                        methodInfo.HasAttribute(context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute")))
                    {
                        return false;
                    }
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
                var otherAwaitExpressions = method.DescendantNodes(_ => true).OfType<AwaitExpressionSyntax>();
                foreach (var expr in otherAwaitExpressions)
                {
                    if (HasPreviousConfigureAwait(expr))
                        return true;

                    bool HasPreviousConfigureAwait(AwaitExpressionSyntax otherAwaitExpression)
                    {
                        if (otherAwaitExpression == node)
                            return false;

                        if (otherAwaitExpression.GetLocation().SourceSpan.Start > node.GetLocation().SourceSpan.Start)
                            return false;

                        if (!IsConfiguredTaskAwaitable(context, otherAwaitExpression))
                            return false;

                        var nodeStatement = node.FirstAncestorOrSelf<StatementSyntax>();
                        var parentStatement = otherAwaitExpression.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
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

        private static bool CanAddConfigureAwait(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax awaitSyntax)
        {
            var awaitExpressionType = context.SemanticModel.GetTypeInfo(awaitSyntax.Expression).Type;
            if (awaitExpressionType == null)
                return false;

            var members = awaitExpressionType.GetMembers("ConfigureAwait");
            return members.OfType<IMethodSymbol>().Any(member => !member.ReturnsVoid && member.TypeParameters.Length == 0 && member.Parameters.Length == 1 && member.Parameters[0].Type.IsBoolean());
        }

        private static bool IsConfiguredTaskAwaitable(SyntaxNodeAnalysisContext context, AwaitExpressionSyntax awaitSyntax)
        {
            var awaitExpressionType = context.SemanticModel.GetTypeInfo(awaitSyntax.Expression).ConvertedType;
            if (awaitExpressionType == null)
                return false;

            var configuredTaskAwaitableType = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
            var configuredTaskAwaitableOfTType = context.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");
            return (configuredTaskAwaitableType != null && configuredTaskAwaitableType.Equals(awaitExpressionType)) ||
                   (configuredTaskAwaitableOfTType != null && configuredTaskAwaitableOfTType.Equals(awaitExpressionType.OriginalDefinition));
        }
    }
}
