using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeMemberReadOnlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.MakeStructMemberReadOnly,
        title: "Make member readonly",
        messageFormat: "Make '{0}' readonly",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeStructMemberReadOnly));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolStartAction(ctx =>
        {
            var symbol = (INamedTypeSymbol)ctx.Symbol;
            if (!CouldBeReadOnly(symbol))
                return;

            ctx.RegisterOperationBlockStartAction(ctx =>
            {
                if (!CouldBeReadOnly(ctx.OwningSymbol))
                    return;

                if (ctx.OperationBlocks.Length > 0)
                {
                    if (!EnsureLanguageVersion(ctx.OperationBlocks[0]))
                        return;
                }

                var analyzerContext = new AnalyzerContext();
                ctx.RegisterOperationAction(analyzerContext.AnalyzeBlock, OperationKind.Block);
                ctx.RegisterOperationBlockEndAction(analyzerContext.AnalyzeEnd);
            });
        }, SymbolKind.NamedType);
    }

    private sealed class AnalyzerContext
    {
        private bool _canBeReadOnly = true;

        public void AnalyzeBlock(OperationAnalysisContext context)
        {
            var operation = (IBlockOperation)context.Operation;
            var semanticModel = operation.SemanticModel!;

            var arg = GetDataFlowArgument(operation.Syntax);
            if (arg == null)
                return;

            var dataFlow = semanticModel.AnalyzeDataFlow(arg);
            foreach (var symbol in dataFlow.WrittenInside)
            {
                if (symbol is IParameterSymbol parameter && parameter.IsThis)
                {
                    _canBeReadOnly = false;
                }
            }
        }

        public void AnalyzeEnd(OperationBlockAnalysisContext context)
        {
            if (_canBeReadOnly)
            {
                if (context.OwningSymbol is IMethodSymbol method && method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
                {
                    var parent = context.OperationBlocks.FirstOrDefault()?.Syntax.Parent;
                    if (parent?.IsKind(SyntaxKind.PropertyDeclaration) == true)
                    {
                        context.ReportDiagnostic(s_rule, ((PropertyDeclarationSyntax)parent).Identifier, context.OwningSymbol.Name);
                        return;
                    }
                }

                context.ReportDiagnostic(s_rule, context.OwningSymbol, context.OwningSymbol.Name);
            }
        }

        private static SyntaxNode? GetDataFlowArgument(SyntaxNode node)
        {
            if (node == null)
                return null;

            if (node is ArrowExpressionClauseSyntax expression)
            {
                return expression.Expression;
            }

            return node;
        }
    }

    private static bool EnsureLanguageVersion(IOperation operation)
    {
        // Readonly instance members are available with C# 8
        return operation.Syntax.SyntaxTree.Options is CSharpParseOptions options && options.LanguageVersion >= LanguageVersion.CSharp8;
    }

    private static bool CouldBeReadOnly(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            if (method.IsReadOnly || method.IsStatic)
                return false;

            if (method.MethodKind is MethodKind.Ordinary or MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.ExplicitInterfaceImplementation)
                return CouldBeReadOnly(symbol.ContainingType);
        }

        return false;
    }

    private static bool CouldBeReadOnly(INamedTypeSymbol symbol)
    {
        return symbol.IsValueType && !symbol.IsReadOnly;
    }
}
