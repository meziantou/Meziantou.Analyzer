using System.Collections.Concurrent;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeMemberReadOnlyAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MakeStructMemberReadOnly,
        title: "Make member readonly",
        messageFormat: "Make '{0}' readonly",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MakeStructMemberReadOnly));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolStartAction(ctx =>
        {
            var symbol = (INamedTypeSymbol)ctx.Symbol;
            if (!CouldBeReadOnly(symbol))
                return;

            // 'readonly' cannot be applied to an event accessor, only to the event itself, so the accessors are
            // collected and the event is reported once every one of them can be readonly
            var readOnlyEventAccessors = new ConcurrentDictionary<IEventSymbol, ConcurrentHashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

            ctx.RegisterOperationBlockStartAction(ctx =>
            {
                if (!CouldBeReadOnly(ctx.OwningSymbol))
                    return;

                if (ctx.OperationBlocks.Length > 0)
                {
                    if (!EnsureLanguageVersion(ctx.OperationBlocks[0]))
                        return;
                }

                var analyzerContext = new AnalyzerContext(readOnlyEventAccessors);
                ctx.RegisterOperationAction(analyzerContext.AnalyzeBlock, OperationKind.Block);
                ctx.RegisterOperationBlockEndAction(analyzerContext.AnalyzeEnd);
            });

            ctx.RegisterSymbolEndAction(ctx => ReportEvents(ctx, readOnlyEventAccessors));
        }, SymbolKind.NamedType);
    }

    private static void ReportEvents(SymbolAnalysisContext context, ConcurrentDictionary<IEventSymbol, ConcurrentHashSet<IMethodSymbol>> readOnlyEventAccessors)
    {
        foreach (var (eventSymbol, readOnlyAccessors) in readOnlyEventAccessors)
        {
            var accessorCount = 0;
            if (eventSymbol.AddMethod is not null)
            {
                accessorCount++;
            }

            if (eventSymbol.RemoveMethod is not null)
            {
                accessorCount++;
            }

            if (readOnlyAccessors.Count != accessorCount)
                continue;

            foreach (var syntaxReference in eventSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(context.CancellationToken) is EventDeclarationSyntax eventDeclaration)
                {
                    context.ReportDiagnostic(Rule, eventDeclaration.Identifier, [eventSymbol.Name]);
                }
            }
        }
    }

    private sealed class AnalyzerContext(ConcurrentDictionary<IEventSymbol, ConcurrentHashSet<IMethodSymbol>> readOnlyEventAccessors)
    {
        private bool _canBeReadOnly = true;

        public void AnalyzeBlock(OperationAnalysisContext context)
        {
            var operation = (IBlockOperation)context.Operation;
            var semanticModel = operation.SemanticModel!;

            var arg = GetDataFlowArgument(operation.Syntax);
            if (arg is null)
                return;

            var dataFlow = semanticModel.AnalyzeDataFlow(arg);
            foreach (var symbol in dataFlow.WrittenInside)
            {
                if (symbol is IParameterSymbol parameter && parameter.IsThis)
                {
                    _canBeReadOnly = false;
                }
            }

            foreach (var symbol in dataFlow.UnsafeAddressTaken)
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
                if (context.OwningSymbol is IMethodSymbol method)
                {
                    if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
                    {
                        var parent = context.OperationBlocks.FirstOrDefault()?.Syntax.Parent;
                        if (parent?.IsKind(SyntaxKind.PropertyDeclaration) == true)
                        {
                            context.ReportDiagnostic(Rule, ((PropertyDeclarationSyntax)parent).Identifier, context.OwningSymbol.Name);
                            return;
                        }
                    }
                    else if (method.MethodKind is MethodKind.EventAdd or MethodKind.EventRemove)
                    {
                        // The event is reported by ReportEvents once all its accessors can be readonly
                        if (method.AssociatedSymbol is IEventSymbol eventSymbol)
                        {
                            readOnlyEventAccessors.GetOrAdd(eventSymbol, _ => []).Add(method);
                        }

                        return;
                    }
                }

                context.ReportDiagnostic(Rule, context.OwningSymbol, context.OwningSymbol.Name);
            }
        }

        private static SyntaxNode? GetDataFlowArgument(SyntaxNode node)
        {
            if (node is null)
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
        return operation.GetCSharpLanguageVersion().IsCSharp8OrGreater();
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
