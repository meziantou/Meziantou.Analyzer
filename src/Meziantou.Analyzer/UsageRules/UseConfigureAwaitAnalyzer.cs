﻿using System.Collections.Immutable;
using System.Linq;
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
                    if (InheritsFrom(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Threading.DispatcherObject")) || // WPF
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Input.ICommand")) || // WPF
                        InheritsFrom(symbol, context.Compilation.GetTypeByMetadataName("System.Windows.Forms.Control")) || // WinForms
                        InheritsFrom(symbol, context.Compilation.GetTypeByMetadataName("System.Web.UI.WebControls.WebControl")) || // ASP.NET (Webforms)
                        InheritsFrom(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase")) || // ASP.NET Core (as there is no SynchronizationContext, ConfigureAwait(false) is useless)
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper")) || // ASP.NET Core
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent")) || // ASP.NET Core
                        Implements(symbol, context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata")))  // ASP.NET Core
                    {
                        return false;
                    }
                }
            }

            var containingMethod = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (containingMethod != null)
            {
                var methodInfo = context.SemanticModel.GetSymbolInfo(containingMethod).Symbol as IMethodSymbol;
                if (methodInfo != null)
                {
                    if (MethodHasAttribute(methodInfo, context.Compilation.GetTypeByMetadataName("XUnit.FactAttribute")) ||
                        MethodHasAttribute(methodInfo, context.Compilation.GetTypeByMetadataName("NUnit.Framework.NUnitAttribute")) ||
                        MethodHasAttribute(methodInfo, context.Compilation.GetTypeByMetadataName("NUnit.Framework.TheoryAttribute")) ||
                        MethodHasAttribute(methodInfo, context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")) ||
                        MethodHasAttribute(methodInfo, context.Compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute")))
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

        private static bool InheritsFrom(ITypeSymbol classSymbol, ITypeSymbol baseClassType)
        {
            if (baseClassType == null)
                return false;

            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (baseClassType.Equals(baseType))
                    return true;

                baseType = baseType.BaseType;
            }

            return false;
        }

        private static bool Implements(ITypeSymbol classSymbol, ITypeSymbol interfaceType)
        {
            if (interfaceType == null)
                return false;

            return classSymbol.AllInterfaces.Any(i => interfaceType.Equals(i));
        }

        public static bool MethodHasAttribute(ISymbol method, ITypeSymbol attributeType)
        {
            if (attributeType == null)
                return false;

            var attributes = method.GetAttributes();
            if (attributes == null)
                return false;

            foreach (var attribute in attributes)
            {
                if (attributeType.Equals(attribute.AttributeClass))
                    return true;
            }

            return false;
        }
    }
}
