using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MakeMethodStaticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_methodRule = new DiagnosticDescriptor(
            RuleIdentifiers.MakeMethodStatic,
            title: "Make method static",
            messageFormat: "Make method static",
            RuleCategories.Design,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeMethodStatic));

        private static readonly DiagnosticDescriptor s_propertyRule = new DiagnosticDescriptor(
         RuleIdentifiers.MakePropertyStatic,
         title: "Make property static",
         messageFormat: "Make property static",
         RuleCategories.Design,
         DiagnosticSeverity.Info,
         isEnabledByDefault: true,
         description: "",
         helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakePropertyStatic));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_methodRule, s_propertyRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(ctx =>
            {
                var analyzerContext = new AnalyzerContext();

                ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeMethod, SyntaxKind.MethodDeclaration);
                ctx.RegisterSyntaxNodeAction(analyzerContext.AnalyzeProperty, SyntaxKind.PropertyDeclaration);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeEventAssignment, OperationKind.EventAssignment);
                ctx.RegisterCompilationEndAction(analyzerContext.CompilationEnd);
            });
        }

        private sealed class AnalyzerContext
        {
            private readonly HashSet<ISymbol> _potentialSymbols = new HashSet<ISymbol>();
            private readonly HashSet<ISymbol> _cannotBeStaticSymbols = new HashSet<ISymbol>();

            public void CompilationEnd(CompilationAnalysisContext context)
            {
                foreach (var symbol in _potentialSymbols)
                {
                    if (_cannotBeStaticSymbols.Contains(symbol))
                        continue;

                    if (symbol is IMethodSymbol)
                    {
                        context.ReportDiagnostic(s_methodRule, symbol);
                    }
                    else if (symbol is IPropertySymbol)
                    {
                        context.ReportDiagnostic(s_propertyRule, symbol);
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
                var methodSymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) as IMethodSymbol;
                if (methodSymbol == null)
                    return;

                if (!IsPotentialStatic(methodSymbol) ||
                    methodSymbol.IsUnitTestMethod() ||
                    IsAspNetCoreMiddleware(context.Compilation, methodSymbol) ||
                    IsAspNetCoreStartup(context.Compilation, methodSymbol))
                {
                    return;
                }

                var body = (SyntaxNode)node.Body ?? node.ExpressionBody;
                if (body == null)
                    return;

                var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
                if (operation == null || HasInstanceUsages(operation))
                    return;

                lock (_potentialSymbols)
                {
                    _potentialSymbols.Add(methodSymbol);
                }
            }

            public void AnalyzeProperty(SyntaxNodeAnalysisContext context)
            {
                var node = (PropertyDeclarationSyntax)context.Node;
                var propertySymbol = context.SemanticModel.GetDeclaredSymbol(node, context.CancellationToken) as IPropertySymbol;
                if (propertySymbol == null)
                    return;

                if (!IsPotentialStatic(propertySymbol))
                    return;

                if (node.ExpressionBody != null)
                {
                    var operation = context.SemanticModel.GetOperation(node.ExpressionBody, context.CancellationToken);
                    if (operation == null || HasInstanceUsages(operation))
                        return;
                }

                if (node.AccessorList != null)
                {
                    foreach (var accessor in node.AccessorList.Accessors)
                    {
                        var body = (SyntaxNode)accessor.Body ?? accessor.ExpressionBody;
                        if (body == null)
                            return;

                        var operation = context.SemanticModel.GetOperation(body, context.CancellationToken);
                        if (operation == null || HasInstanceUsages(operation))
                            return;
                    }
                }

                lock (_potentialSymbols)
                {
                    _potentialSymbols.Add(propertySymbol);
                }
            }

            public void AnalyzeEventAssignment(OperationAnalysisContext context)
            {
                var operation = (IEventAssignmentOperation)context.Operation;
                var delegateOperation = operation.HandlerValue as IDelegateCreationOperation;
                if (delegateOperation == null)
                    return;

                var methodReference = delegateOperation.Target as IMethodReferenceOperation;
                if (methodReference == null)
                    return;

                // xaml cannot add event to static methods
                if (IsInXamlGeneratedFile(operation))
                {
                    lock (_cannotBeStaticSymbols)
                    {
                        _cannotBeStaticSymbols.Add(methodReference.Method);
                    }
                }
            }

            private static bool IsPotentialStatic(IMethodSymbol symbol)
            {
                return
                    !symbol.IsAbstract &&
                    !symbol.IsVirtual &&
                    !symbol.IsOverride &&
                    !symbol.IsStatic &&
                    !symbol.IsInterfaceImplementation();
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
                if (operation == null)
                    return false;

                var operations = new Queue<IOperation>();
                operations.Enqueue(operation);

                while (operations.Count > 0)
                {
                    var op = operations.Dequeue();
                    foreach (var child in op.Children)
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

            private static bool IsAspNetCoreMiddleware(Compilation compilation, IMethodSymbol methodSymbol)
            {
                if (string.Equals(methodSymbol.Name, "Invoke", StringComparison.Ordinal) ||
                    string.Equals(methodSymbol.Name, "InvokeAsync", StringComparison.Ordinal))
                {
                    var httpContextSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
                    if (methodSymbol.Parameters.Length == 0 || !methodSymbol.Parameters[0].Type.IsEqualTo(httpContextSymbol))
                        return false;

                    return true;
                }

                var imiddlewareSymbol = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IMiddleware");
                if (imiddlewareSymbol != null)
                {
                    if (methodSymbol.ContainingType.Implements(imiddlewareSymbol))
                    {
                        var invokeAsyncSymbol = imiddlewareSymbol.GetMembers("InvokeAsync").FirstOrDefault();
                        if (invokeAsyncSymbol != null)
                        {
                            var implementationMember = methodSymbol.ContainingType.FindImplementationForInterfaceMember(invokeAsyncSymbol);
                            if (methodSymbol.IsEqualTo(implementationMember))
                                return true;
                        }
                    }
                }

                return false;
            }

            private static bool IsAspNetCoreStartup(Compilation compilation, IMethodSymbol methodSymbol)
            {
                // void ConfigureServices Microsoft.Extensions.DependencyInjection.IServiceCollection
                if (string.Equals(methodSymbol.Name, "ConfigureServices", StringComparison.Ordinal))
                {
                    var iserviceCollectionSymbol = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.IServiceCollection");
                    if (methodSymbol.ReturnsVoid && methodSymbol.Parameters.Length == 1 && methodSymbol.Parameters[0].Type.IsEqualTo(iserviceCollectionSymbol))
                        return true;

                    return false;
                }

                // void Configure Microsoft.AspNetCore.Builder.IApplicationBuilder
                if (string.Equals(methodSymbol.Name, "Configure", StringComparison.Ordinal))
                {
                    var iapplicationBuilder = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.IApplicationBuilder");
                    if (methodSymbol.Parameters.Length > 0 && methodSymbol.Parameters[0].Type.IsEqualTo(iapplicationBuilder))
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
}
