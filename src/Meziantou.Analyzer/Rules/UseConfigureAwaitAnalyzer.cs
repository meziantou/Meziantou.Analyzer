using System.Collections.Concurrent;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseConfigureAwaitAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseConfigureAwaitFalse,
        title: "Use Task.ConfigureAwait",
        messageFormat: "Use Task.ConfigureAwait(false) if the current SynchronizationContext is not needed",
        RuleCategories.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseConfigureAwaitFalse),
        customTags: [GeneratedCodeReporting.ReportInGeneratedCodeTag]);

    private static readonly ConfigurationDefinition<string> ReportModeConfiguration = new(RuleIdentifiers.UseConfigureAwaitFalse + ".report", defaultValue: "");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);
            ctx.RegisterOperationBlockStartAction(analyzerContext.AnalyzeOperationBlockStart);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private INamedTypeSymbol? ConfiguredAsyncDisposableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredAsyncDisposable");

        private INamedTypeSymbol? IAsyncEnumerableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        private INamedTypeSymbol? ConfiguredCancelableAsyncEnumerableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable`1");
        private INamedTypeSymbol? ConfiguredTaskAwaitableSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable");
        private INamedTypeSymbol? ConfiguredTaskAwaitableOfTSymbol { get; } = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1");

        private INamedTypeSymbol? WPF_DispatcherObject { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Threading.DispatcherObject");
        private INamedTypeSymbol? WPF_ICommand { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Input.ICommand");
        private INamedTypeSymbol? WinForms_Control { get; } = compilation.GetBestTypeByMetadataName("System.Windows.Forms.Control");
        private INamedTypeSymbol? WebForms_WebControl { get; } = compilation.GetBestTypeByMetadataName("System.Web.UI.WebControls.WebControl");
        private INamedTypeSymbol? AspNetCore_ControllerBase { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        private INamedTypeSymbol? AspNetCore_IRazorPage { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Mvc.Razor.IRazorPage");
        private INamedTypeSymbol? AspNetCore_ITagHelper { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelper");
        private INamedTypeSymbol? AspNetCore_ITagHelperComponent { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Razor.TagHelpers.ITagHelperComponent");
        private INamedTypeSymbol? AspNetCore_IFilterMetadata { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata");
        private INamedTypeSymbol? AspNetCore_IComponent { get; } = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");

        public void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
        {
            // The containing type is the same for all the operations of the block, so the framework types
            // are only checked once per operation block instead of once per await
            var blockContext = new OperationBlockContext(this, HasSynchronizationContext(context.OwningSymbol.ContainingType));
            context.RegisterOperationAction(blockContext.AnalyzeAwaitOperation, OperationKind.Await);
            context.RegisterOperationAction(blockContext.AnalyzeForEachStatement, OperationKind.Loop);
            context.RegisterOperationAction(blockContext.AnalyzeUsingOperation, OperationKind.Using);
            context.RegisterOperationAction(blockContext.AnalyzeUsingDeclarationOperation, OperationKind.UsingDeclaration);
        }

        private bool HasSynchronizationContext(INamedTypeSymbol? containingType)
        {
            if (containingType is null)
                return false;

            return containingType.InheritsFrom(WPF_DispatcherObject) ||
                   containingType.Implements(WPF_ICommand) ||
                   containingType.InheritsFrom(WinForms_Control) || // WinForms
                   containingType.InheritsFrom(WebForms_WebControl) || // ASP.NET (Webforms)
                   containingType.InheritsFrom(AspNetCore_ControllerBase) || // ASP.NET Core (as there is no SynchronizationContext, ConfigureAwait(false) is useless)
                   containingType.Implements(AspNetCore_IRazorPage) || // ASP.NET Core
                   containingType.Implements(AspNetCore_ITagHelper) || // ASP.NET Core
                   containingType.Implements(AspNetCore_ITagHelperComponent) || // ASP.NET Core
                   containingType.Implements(AspNetCore_IFilterMetadata) ||
                   containingType.Implements(AspNetCore_IComponent); // Blazor has a synchronization context, see https://github.com/meziantou/Meziantou.Analyzer/issues/96
        }

        public bool IsConfiguredAsyncDisposable(ITypeSymbol type) => type.IsEqualTo(ConfiguredAsyncDisposableSymbol);

        public bool IsConfiguredCancelableAsyncEnumerable(ITypeSymbol type) => type.OriginalDefinition.IsEqualTo(ConfiguredCancelableAsyncEnumerableSymbol);

        public bool IsAsyncEnumerable(ITypeSymbol? type) => type.IsEqualTo(IAsyncEnumerableSymbol);

        public bool IsConfiguredTaskAwaitable(SemanticModel semanticModel, AwaitExpressionSyntax awaitSyntax, CancellationToken cancellationToken)
        {
            var awaitExpressionType = semanticModel.GetTypeInfo(awaitSyntax.Expression, cancellationToken).ConvertedType;
            if (awaitExpressionType is null)
                return false;

            return ConfiguredTaskAwaitableSymbol.IsEqualTo(awaitExpressionType) ||
                   ConfiguredTaskAwaitableOfTSymbol.IsEqualTo(awaitExpressionType.OriginalDefinition);
        }
    }

    private sealed class OperationBlockContext(AnalyzerContext analyzerContext, bool hasSynchronizationContext)
    {
        private ConfiguredAwaits? _configuredAwaits;
        private ConcurrentDictionary<StatementSyntax, bool>? _endPointIsReachable;

        public void AnalyzeAwaitOperation(OperationAnalysisContext context)
        {
            // if expression is of type ConfiguredTaskAwaitable, do nothing
            // If ConfigureAwait(false) somewhere in a method, all following await calls should have ConfigureAwait(false)
            // Use ConfigureAwait(false) everywhere except if the parent class is a WPF, Winform, or ASP.NET class, or ASP.NET Core (because there is no SynchronizationContext)
            var operation = (IAwaitOperation)context.Operation;

            var awaitedOperationType = operation.Operation.Type;
            if (awaitedOperationType is null || operation.SemanticModel is null)
                return;

            if (!CanAddConfigureAwait(awaitedOperationType, operation))
                return;

            if (MustUseConfigureAwait(operation.SemanticModel, context.Options, operation.Operation.Syntax, context.CancellationToken))
            {
                context.ReportDiagnostic(Rule, operation);
            }
        }

        public void AnalyzeForEachStatement(OperationAnalysisContext context)
        {
            if (context.Operation is not IForEachLoopOperation operation)
                return;

            if (!operation.IsAsynchronous)
                return;

            // ConfiguredCancelableAsyncEnumerable
            var collectionType = operation.Collection.GetActualType(context.CancellationToken);
            if (collectionType is null)
                return;

            if (analyzerContext.IsConfiguredCancelableAsyncEnumerable(collectionType))
            {
                // Enumerable().WithCancellation(ct) or Enumerable().ConfigureAwait(false)
                if (HasConfigureAwait(operation.Collection) && HasPartOfTypeIAsyncEnumerable(operation.Collection))
                    return;

                // Check if it's a variable reference that is already configured
                // note: this doesn't check if the value is well-configured
                // https://github.com/meziantou/Meziantou.Analyzer/issues/232
                if (operation.Collection.UnwrapImplicitConversions() is ILocalReferenceOperation)
                    return;
            }

            if (!CanAddConfigureAwait(collectionType, operation.Collection))
                return;

            if (MustUseConfigureAwait(operation.SemanticModel!, context.Options, operation.Syntax, context.CancellationToken))
            {
                var data = ImmutableDictionary<string, string?>.Empty.Add(UseConfigureAwaitAnalyzerCommon.KindKey, "foreach");
                context.ReportDiagnostic(Rule, data, operation.Collection);
            }

            static bool HasConfigureAwait(IOperation operation)
            {
                if (operation is IInvocationOperation invocation)
                {
                    if (invocation.TargetMethod.Name == "ConfigureAwait")
                        return true;
                }

                foreach (var child in operation.GetChildOperations())
                {
                    if (HasConfigureAwait(child))
                        return true;
                }

                return false;
            }

            bool HasPartOfTypeIAsyncEnumerable(IOperation operation)
            {
                if (analyzerContext.IsAsyncEnumerable(operation.Type))
                    return true;

                foreach (var child in operation.GetChildOperations())
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

            var resources = operation.Resources;
            if (resources is IVariableDeclarationGroupOperation declarationGroup)
            {
                // await using(var a = expr, b = expr)
                AnalyzeVariableDeclarationGroupOperation(context, declarationGroup);
            }
            else
            {
                // await using(expr)
                if (resources.Type is null)
                    return;

                if (!CanAddConfigureAwait(resources.Type, resources))
                    return;

                if (MustUseConfigureAwait(resources.SemanticModel!, context.Options, resources.Syntax, context.CancellationToken))
                {
                    var properties = ImmutableDictionary<string, string?>.Empty.Add(UseConfigureAwaitAnalyzerCommon.KindKey, "using");
                    context.ReportDiagnostic(Rule, properties, resources);
                }
            }
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
                    if (declarator.Initializer is null)
                        continue;

                    // ConfiguredCancelableAsyncEnumerable
                    var variableType = declarator.Initializer.Value.GetActualType(context.CancellationToken);
                    if (variableType is null || analyzerContext.IsConfiguredAsyncDisposable(variableType))
                        return;

                    if (!CanAddConfigureAwait(variableType, declarator.Initializer.Value))
                        return;

                    if (MustUseConfigureAwait(declarator.SemanticModel!, context.Options, declarator.Syntax, context.CancellationToken))
                    {
                        context.ReportDiagnostic(Rule, declarator);
                    }
                }
            }
        }

        private bool MustUseConfigureAwait(SemanticModel semanticModel, AnalyzerOptions options, SyntaxNode node, CancellationToken cancellationToken)
        {
            var modeValue = options.GetConfigurationValue(node.SyntaxTree, ReportModeConfiguration);
            if (Enum.TryParse<ReportMode>(modeValue, ignoreCase: true, out var mode))
            {
                if (mode == ReportMode.Always)
                    return true;
            }

            // The context detection can only prevent the diagnostic from being reported, and a previous
            // ConfigureAwait(false) in the same method overrides it. So, when the code is not in a context that
            // has a SynchronizationContext, the result is the same whatever the previous awaits are, and there is
            // no need to run the expensive control flow analysis of HasPreviousConfigureAwait.
            if (!hasSynchronizationContext && !IsInUnitTestMethod(semanticModel, node, cancellationToken))
                return true;

            // If ConfigureAwait(false) is used somewhere in the method, all the following awaits should use it too
            return HasPreviousConfigureAwait(semanticModel, node, cancellationToken);
        }

        private static bool IsInUnitTestMethod(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            var containingMethod = GetParentSymbol<IMethodSymbol>(semanticModel, node, cancellationToken);
            return containingMethod is not null && containingMethod.IsUnitTestMethod();
        }

        private bool HasPreviousConfigureAwait(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            // Find all previous awaits with ConfiguredAwait(false)
            // Use semanticModel.AnalyzeControlFlow to check if the current await is accessible from one of the previous await
            // https://joshvarty.com/2015/03/24/learn-roslyn-now-control-flow-analysis/
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method is null)
                return false;

            var configuredAwaits = GetConfiguredAwaits(semanticModel, method, cancellationToken);
            if (configuredAwaits.Length == 0)
                return false;

            var nodeStart = node.SpanStart;
            var nodeStatement = node.FirstAncestorOrSelf<StatementSyntax>();
            foreach (var otherAwaitExpression in configuredAwaits)
            {
                // The awaits are ordered by position, so the next ones cannot be before the current node
                if (otherAwaitExpression.SpanStart > nodeStart)
                    break;

                if (otherAwaitExpression == node)
                    continue;

                if (IsReachableFrom(semanticModel, otherAwaitExpression, nodeStatement))
                    return true;
            }

            return false;
        }

        private bool IsReachableFrom(SemanticModel semanticModel, AwaitExpressionSyntax otherAwaitExpression, StatementSyntax? nodeStatement)
        {
            var parentStatement = otherAwaitExpression.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            while (parentStatement is not null && nodeStatement != parentStatement)
            {
                if (!IsEndPointReachable(semanticModel, parentStatement))
                    return false;

                parentStatement = parentStatement.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            }

            return true;
        }

        private AwaitExpressionSyntax[] GetConfiguredAwaits(SemanticModel semanticModel, MethodDeclarationSyntax method, CancellationToken cancellationToken)
        {
            // All the operations of a block belong to the same method, so the awaits of the method are only
            // enumerated once instead of once per await
            var cached = _configuredAwaits;
            if (cached is not null && cached.Method == method)
                return cached.Awaits;

            List<AwaitExpressionSyntax>? awaits = null;
            foreach (var awaitExpression in method.DescendantNodes(_ => true).OfType<AwaitExpressionSyntax>())
            {
                if (analyzerContext.IsConfiguredTaskAwaitable(semanticModel, awaitExpression, cancellationToken))
                {
                    awaits ??= [];
                    awaits.Add(awaitExpression);
                }
            }

            var result = awaits is null ? [] : awaits.ToArray();
            _configuredAwaits = new ConfiguredAwaits(method, result);
            return result;
        }

        private bool IsEndPointReachable(SemanticModel semanticModel, StatementSyntax statementSyntax)
        {
            // The same statements are walked over and over while looking for the awaits of a method
            var cache = _endPointIsReachable ??= new ConcurrentDictionary<StatementSyntax, bool>();
            if (cache.TryGetValue(statementSyntax, out var isReachable))
                return isReachable;

            var result = semanticModel.AnalyzeControlFlow(statementSyntax);
            isReachable = result is not null && result.Succeeded && result.EndPointIsReachable;
            cache[statementSyntax] = isReachable;
            return isReachable;
        }

        private static bool CanAddConfigureAwait(ITypeSymbol awaitedType, IOperation operation)
        {
            return CanAddConfigureAwait(awaitedType, operation.SemanticModel!, operation.Syntax);
        }

        private static bool CanAddConfigureAwait(ITypeSymbol awaitedType, SemanticModel semanticModel, SyntaxNode node)
        {
            var location = node.Span.End;
            var result = semanticModel.LookupSymbols(location, container: awaitedType, name: "ConfigureAwait", includeReducedExtensionMethods: true);
            if (result.Length > 0)
                return true;

            return false;
        }

        private static T? GetParentSymbol<T>(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken) where T : class, ISymbol
        {
            var symbol = semanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken);
            while (symbol is not null)
            {
                if (symbol is T expectedSymbol)
                    return expectedSymbol;

                symbol = symbol.ContainingSymbol;
            }

            return default;
        }

        private sealed class ConfiguredAwaits(MethodDeclarationSyntax method, AwaitExpressionSyntax[] awaits)
        {
            public MethodDeclarationSyntax Method { get; } = method;
            public AwaitExpressionSyntax[] Awaits { get; } = awaits;
        }
    }

    private enum ReportMode
    {
        DetectContext,
        Always,
    }
}
