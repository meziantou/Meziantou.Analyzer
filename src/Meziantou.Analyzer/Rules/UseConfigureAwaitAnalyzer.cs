using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseConfigureAwaitAnalyzer : DiagnosticAnalyzer
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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext(ctx.Compilation);
                ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeForEachStatement, OperationKind.Loop);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeUsingOperation, OperationKind.Using);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeUsingDeclarationOperation, OperationKind.UsingDeclaration);
            });
        }

        private sealed class AnalyzerContext
        {
            public AnalyzerContext(Compilation compilation)
            {
                IAsyncDisposableSymbol = compilation.GetTypeByMetadataName("System.IAsyncDisposable");
                ConfiguredAsyncDisposableSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredAsyncDisposable");
                IAsyncDisposable_ConfigureAwaitSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskAsyncEnumerableExtensions")?.GetMembers("ConfigureAwait").FirstOrDefault() as IMethodSymbol;

                IAsyncEnumerableSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
                IAsyncEnumerable_ConfigureAwaitSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskAsyncEnumerableExtensions")?.GetMembers("ConfigureAwait").FirstOrDefault() as IMethodSymbol;
                ConfiguredCancelableAsyncEnumerableSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1");
                ConfiguredTaskAwaitableSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
                ConfiguredTaskAwaitableOfTSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");

                WPF_DispatcherObject = compilation.GetTypeByMetadataName("System.Windows.Threading.DispatcherObject");
                WPF_ICommand = compilation.GetTypeByMetadataName("System.Windows.Input.ICommand");

                WinForms_Control = compilation.GetTypeByMetadataName("System.Windows.Forms.Control");
                WebForms_WebControl = compilation.GetTypeByMetadataName("System.Web.UI.WebControls.WebControl");
                AspNetCore_ControllerBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
                AspNetCore_IRazorPage = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Razor.IRazorPage");
                AspNetCore_ITagHelper = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper");
                AspNetCore_ITagHelperComponent = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent");
                AspNetCore_IFilterMetadata = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata");
                AspNetCore_IComponent = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
            }

            private INamedTypeSymbol? IAsyncDisposableSymbol { get; }
            private INamedTypeSymbol? ConfiguredAsyncDisposableSymbol { get; }
            private IMethodSymbol? IAsyncDisposable_ConfigureAwaitSymbol { get; }

            private INamedTypeSymbol? IAsyncEnumerableSymbol { get; }
            private IMethodSymbol? IAsyncEnumerable_ConfigureAwaitSymbol { get; }
            private INamedTypeSymbol? ConfiguredCancelableAsyncEnumerableSymbol { get; }
            private INamedTypeSymbol? ConfiguredTaskAwaitableSymbol { get; }
            private INamedTypeSymbol? ConfiguredTaskAwaitableOfTSymbol { get; }

            private INamedTypeSymbol? WPF_DispatcherObject { get; }
            private INamedTypeSymbol? WPF_ICommand { get; }
            private INamedTypeSymbol? WinForms_Control { get; }
            private INamedTypeSymbol? WebForms_WebControl { get; }
            private INamedTypeSymbol? AspNetCore_ControllerBase { get; }
            private INamedTypeSymbol? AspNetCore_IRazorPage { get; }
            private INamedTypeSymbol? AspNetCore_ITagHelper { get; }
            private INamedTypeSymbol? AspNetCore_ITagHelperComponent { get; }
            private INamedTypeSymbol? AspNetCore_IFilterMetadata { get; }
            private INamedTypeSymbol? AspNetCore_IComponent { get; }

            public void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
            {
                // if expression is of type ConfiguredTaskAwaitable, do nothing
                // If ConfigureAwait(false) somewhere in a method, all following await calls should have ConfigureAwait(false)
                // Use ConfigureAwait(false) everywhere except if the parent class is a WPF, Winform, or ASP.NET class, or ASP.NET Core (because there is no SynchronizationContext)
                var node = (AwaitExpressionSyntax)context.Node;
                if (!CanAddConfigureAwait(context.SemanticModel, node, context.CancellationToken))
                    return;

                if (MustUseConfigureAwait(context.SemanticModel, node, context.CancellationToken))
                {
                    context.ReportDiagnostic(s_rule, context.Node);
                }
            }

            public void AnalyzeForEachStatement(OperationAnalysisContext context)
            {
                var operation = context.Operation as IForEachLoopOperation;
                if (operation == null)
                    return;

                if (!operation.IsAsynchronous)
                    return;

                // ConfiguredCancelableAsyncEnumerable
                var collectionType = operation.Collection.GetActualType();
                if (collectionType.OriginalDefinition.IsEqualTo(ConfiguredCancelableAsyncEnumerableSymbol))
                {
                    // Enumerable().WithCancellation(ct) or Enumerable().ConfigureAwait(false)
                    if (HasConfigureAwait(operation.Collection) && HasPartOfTypeIAsyncEnumerable(operation.Collection))
                        return;
                }

                if (!CanAddConfigureAwait(collectionType))
                    return;

                if (MustUseConfigureAwait(operation.SemanticModel, operation.Syntax, context.CancellationToken))
                {
                    context.ReportDiagnostic(s_rule, operation.Collection);
                }

                static bool HasConfigureAwait(IOperation operation)
                {
                    if (operation is IInvocationOperation invocation)
                    {
                        if (invocation.TargetMethod.Name == "ConfigureAwait")
                            return true;
                    }

                    foreach (var child in operation.Children)
                    {
                        if (HasConfigureAwait(child))
                            return true;
                    }

                    return false;
                }

                bool HasPartOfTypeIAsyncEnumerable(IOperation operation)
                {
                    if (operation.Type.IsEqualTo(IAsyncEnumerableSymbol))
                        return true;

                    foreach (var child in operation.Children)
                    {
                        if (HasConfigureAwait(child))
                            return true;
                    }

                    return false;
                }
            }

            public void AnalyzeUsingOperation(OperationAnalysisContext context)
            {
                var operation = (IUsingOperation)context.Operation;
                if (!operation.IsAsynchronous)
                    return;

                var declarationGroup = operation.Children.FirstOrDefault() as IVariableDeclarationGroupOperation;
                if (declarationGroup == null)
                    return;

                AnalyzeVariableDeclarationGroupOperation(context, declarationGroup);
            }

            public void AnalyzeUsingDeclarationOperation(OperationAnalysisContext context)
            {
                var operation = (IUsingDeclarationOperation)context.Operation;
                if (!operation.IsAsynchronous)
                    return;

                AnalyzeVariableDeclarationGroupOperation(context, operation.DeclarationGroup);
            }

            private void AnalyzeVariableDeclarationGroupOperation(OperationAnalysisContext context, IVariableDeclarationGroupOperation declarationGroup)
            {
                foreach (var declaration in declarationGroup.Declarations)
                {
                    foreach (var declarator in declaration.Declarators)
                    {
                        if (declarator.Initializer == null)
                            continue;

                        // ConfiguredCancelableAsyncEnumerable
                        var variableType = declarator.Initializer.Value.GetActualType();
                        if (variableType.IsEqualTo(ConfiguredAsyncDisposableSymbol))
                            return;

                        if (!CanAddConfigureAwait(variableType))
                            return;

                        if (MustUseConfigureAwait(declarator.SemanticModel, declarator.Syntax, context.CancellationToken))
                        {
                            context.ReportDiagnostic(s_rule, declarator.Initializer.Value);
                        }
                    }
                }
            }

            private bool MustUseConfigureAwait(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            {
                if (HasPreviousConfigureAwait(semanticModel, node, cancellationToken))
                    return true;

                var containingClass = GetParentSymbol<INamedTypeSymbol>(semanticModel, node, cancellationToken);
                if (containingClass != null)
                {
                    if (containingClass.InheritsFrom(WPF_DispatcherObject) ||
                        containingClass.Implements(WPF_ICommand) ||
                        containingClass.InheritsFrom(WinForms_Control) || // WinForms
                        containingClass.InheritsFrom(WebForms_WebControl) || // ASP.NET (Webforms)
                        containingClass.InheritsFrom(AspNetCore_ControllerBase) || // ASP.NET Core (as there is no SynchronizationContext, ConfigureAwait(false) is useless)
                        containingClass.Implements(AspNetCore_IRazorPage) || // ASP.NET Core
                        containingClass.Implements(AspNetCore_ITagHelper) || // ASP.NET Core
                        containingClass.Implements(AspNetCore_ITagHelperComponent) || // ASP.NET Core
                        containingClass.Implements(AspNetCore_IFilterMetadata) ||
                        containingClass.Implements(AspNetCore_IComponent))  // Blazor has a synchronization context, see https://github.com/meziantou/Meziantou.Analyzer/issues/96
                    {
                        return false;
                    }
                }

                var containingMethod = GetParentSymbol<IMethodSymbol>(semanticModel, node, cancellationToken);
                if (containingMethod != null && containingMethod.IsUnitTestMethod())
                    return false;

                return true;
            }

            private bool HasPreviousConfigureAwait(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
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

                            if (!IsConfiguredTaskAwaitable(semanticModel, otherAwaitExpression, cancellationToken))
                                return false;

                            var nodeStatement = node.FirstAncestorOrSelf<StatementSyntax>();
                            var parentStatement = otherAwaitExpression.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
                            while (parentStatement != null && nodeStatement != parentStatement)
                            {
                                if (!IsEndPointReachable(semanticModel, parentStatement))
                                    return false;

                                parentStatement = parentStatement.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
                            }

                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool IsEndPointReachable(SemanticModel semanticModel, StatementSyntax statementSyntax)
            {
                var result = semanticModel.AnalyzeControlFlow(statementSyntax);
                if (result == null || !result.Succeeded)
                    return false;

                if (!result.EndPointIsReachable)
                    return false;

                return true;
            }

            private bool CanAddConfigureAwait(SemanticModel semanticModel, AwaitExpressionSyntax awaitSyntax, CancellationToken cancellationToken)
            {
                var awaitExpressionType = semanticModel.GetTypeInfo(awaitSyntax.Expression, cancellationToken).Type;
                if (awaitExpressionType == null)
                    return false;

                return CanAddConfigureAwait(awaitExpressionType);
            }

            private bool CanAddConfigureAwait(ITypeSymbol awaitedType)
            {
                var members = awaitedType.GetMembers("ConfigureAwait");
                if (members.OfType<IMethodSymbol>().Any(member => !member.ReturnsVoid && member.TypeParameters.Length == 0 && member.Parameters.Length == 1 && member.Parameters[0].Type.IsBoolean()))
                    return true;

                if (awaitedType.OriginalDefinition.IsEqualTo(IAsyncEnumerableSymbol) && IAsyncEnumerable_ConfigureAwaitSymbol != null)
                    return true;

                if (awaitedType.Implements(IAsyncDisposableSymbol) && IAsyncDisposable_ConfigureAwaitSymbol != null)
                    return true;

                return false;
            }

            private bool IsConfiguredTaskAwaitable(SemanticModel semanticModel, AwaitExpressionSyntax awaitSyntax, CancellationToken cancellationToken)
            {
                var awaitExpressionType = semanticModel.GetTypeInfo(awaitSyntax.Expression, cancellationToken).ConvertedType;
                if (awaitExpressionType == null)
                    return false;

                return ConfiguredTaskAwaitableSymbol.IsEqualTo(awaitExpressionType) ||
                       ConfiguredTaskAwaitableOfTSymbol.IsEqualTo(awaitExpressionType.OriginalDefinition);
            }

            private static T? GetParentSymbol<T>(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken) where T : class, ISymbol
            {
                var symbol = semanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken);
                while (symbol != null)
                {
                    if (symbol is T expectedSymbol)
                        return expectedSymbol;

                    symbol = symbol.ContainingSymbol;
                }

                return default;
            }
        }
    }
}
