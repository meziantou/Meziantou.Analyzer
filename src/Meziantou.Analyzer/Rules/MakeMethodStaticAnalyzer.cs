using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeMethodStaticAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MethodRule = new(
        RuleIdentifiers.MakeMethodStatic,
        title: "Make method static (deprecated, use CA1822 instead)",
        messageFormat: "Make method static (deprecated, use CA1822 instead)",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeMethodStatic));

    private static readonly DiagnosticDescriptor PropertyRule = new(
     RuleIdentifiers.MakePropertyStatic,
     title: "Make property static (deprecated, use CA1822 instead)",
     messageFormat: "Make property static (deprecated, use CA1822 instead)",
     RuleCategories.Design,
     DiagnosticSeverity.Info,
     isEnabledByDefault: true,
     description: "",
     helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakePropertyStatic));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MethodRule, PropertyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(ctx =>
        {
            var analyzerContext = new AnalyzerContext(ctx.Compilation);

            ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeMethod, SyntaxKind.MethodDeclaration);
            ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeProperty, SyntaxKind.PropertyDeclaration);
            ctx.RegisterOperationAction(analyzerContext.AnalyzeDelegateCreation, OperationKind.DelegateCreation);
            ctx.RegisterCompilationEndAction(analyzerContext.CompilationEnd);
        });
    }

    private sealed class AnalyzerContext(Compilation compilation)
    {
        private readonly ConcurrentHashSet<ISymbol> _potentialSymbols = new(SymbolEqualityComparer.Default);
        private readonly ConcurrentHashSet<ISymbol> _cannotBeStaticSymbols = new(SymbolEqualityComparer.Default);

        private readonly ITypeSymbol? _httpContextSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
        private readonly ITypeSymbol? _iapplicationBuilder = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Builder.IApplicationBuilder");
        private readonly ITypeSymbol? _iserviceCollectionSymbol = compilation.GetBestTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
        private readonly ITypeSymbol? _imiddlewareSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Http.IMiddleware");

        public void CompilationEnd(CompilationAnalysisContext context)
        {
            foreach (var symbol in _potentialSymbols)
            {
                if (_cannotBeStaticSymbols.Contains(symbol))
                    continue;

                if (symbol is IMethodSymbol)
                {
                    context.ReportDiagnostic(MethodRule, symbol);
                }
                else if (symbol is IPropertySymbol)
                {
                    context.ReportDiagnostic(PropertyRule, symbol);
                }
                else
                {
                    throw new InvalidOperationException("Symbol is not supported: " + symbol);
                }
            }
        }

        public void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var node = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (methodSymbol is null)
                return;

            if (context.Compilation is null)
                return;

            if (!IsPotentialStatic(methodSymbol) || methodSymbol.IsUnitTestMethod() || IsAspNetCoreMiddleware(methodSymbol) || IsAspNetCoreStartup(methodSymbol))
            {
                return;
            }

            var body = (SyntaxNode?)node.Body ?? node.ExpressionBody;
            if (body is null)
                return;

            var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
            if (operation is null || HasInstanceUsages(operation))
                return;

            _potentialSymbols.Add(methodSymbol);
        }

        public void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var node = (PropertyDeclarationSyntax)context.Node;
            var propertySymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken);
            if (propertySymbol is null)
                return;

            if (!IsPotentialStatic(propertySymbol))
                return;

            if (node.ExpressionBody is not null)
            {
                var operation = context.SemanticModel.GetOperation(node.ExpressionBody, context.CancellationToken);
                if (operation is null || HasInstanceUsages(operation))
                    return;
            }

            if (node.AccessorList is not null)
            {
                foreach (var accessor in node.AccessorList.Accessors)
                {
                    var body = (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody;
                    if (body is null)
                        return;

                    var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
                    if (operation is null || HasInstanceUsages(operation))
                        return;
                }
            }

            _potentialSymbols.Add(propertySymbol);
        }

        public void AnalyzeDelegateCreation(OperationAnalysisContext context)
        {
            var operation = (IDelegateCreationOperation)context.Operation;
            if (operation.Target is not IMethodReferenceOperation methodReference)
                return;

            // xaml cannot add event to static methods
            if (IsInXamlGeneratedFile(operation))
            {
                _cannotBeStaticSymbols.Add(methodReference.Method);
            }
        }

        private static bool IsPotentialStatic(IMethodSymbol symbol)
        {
            return
                !symbol.IsAbstract &&
                !symbol.IsVirtual &&
                !symbol.IsOverride &&
                !symbol.IsStatic &&
                !symbol.IsInterfaceImplementation() &&
                symbol.PartialDefinitionPart is null;
        }

        private static bool IsPotentialStatic(IPropertySymbol symbol)
        {
            return
                !symbol.IsAbstract &&
                !symbol.IsVirtual &&
                !symbol.IsOverride &&
                !symbol.IsStatic &&
                !symbol.IsInterfaceImplementation();
        }

        private static bool HasInstanceUsages(IOperation operation)
        {
            if (operation is null)
                return false;

            var operations = new Queue<IOperation>();
            operations.Enqueue(operation);

            while (operations.Count > 0)
            {
                var op = operations.Dequeue();
                foreach (var child in op.GetChildOperations())
                {
                    operations.Enqueue(child);
                }

                switch (op)
                {
                    case IInstanceReferenceOperation instanceReferenceOperation when instanceReferenceOperation.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance:
                        return true;
                }
            }

            return false;
        }

        private bool IsAspNetCoreMiddleware(IMethodSymbol methodSymbol)
        {
            if (string.Equals(methodSymbol.Name, "Invoke", StringComparison.Ordinal) ||
                string.Equals(methodSymbol.Name, "InvokeAsync", StringComparison.Ordinal))
            {
                if (methodSymbol.Parameters.Length == 0 || !methodSymbol.Parameters[0].Type.IsEqualTo(_httpContextSymbol))
                    return false;

                return true;
            }

            if (methodSymbol.ContainingType.Implements(_imiddlewareSymbol))
            {
                var invokeAsyncSymbol = _imiddlewareSymbol.GetMembers("InvokeAsync").FirstOrDefault();
                if (invokeAsyncSymbol is not null)
                {
                    var implementationMember = methodSymbol.ContainingType.FindImplementationForInterfaceMember(invokeAsyncSymbol);
                    if (methodSymbol.IsEqualTo(implementationMember))
                        return true;
                }
            }

            return false;
        }

        private bool IsAspNetCoreStartup(IMethodSymbol methodSymbol)
        {
            // void ConfigureServices Microsoft.Extensions.DependencyInjection.IServiceCollection
            if (string.Equals(methodSymbol.Name, "ConfigureServices", StringComparison.Ordinal))
            {
                if (methodSymbol.ReturnsVoid && methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.IsEqualTo(_iserviceCollectionSymbol))
                    return true;

                return false;
            }

            // void Configure Microsoft.AspNetCore.Builder.IApplicationBuilder
            if (string.Equals(methodSymbol.Name, "Configure", StringComparison.Ordinal))
            {
                if (methodSymbol.Parameters.Length > 0 && methodSymbol.Parameters[0].Type.IsEqualTo(_iapplicationBuilder))
                    return true;

                return false;
            }

            return false;
        }

        private static bool IsInXamlGeneratedFile(IOperation operation)
        {
            return operation.Syntax.GetLocation().GetMappedLineSpan().Path?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }
}
