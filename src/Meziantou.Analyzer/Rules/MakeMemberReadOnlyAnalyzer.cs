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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolStartAction(ctx =>
        {
            var symbol = (INamedTypeSymbol)ctx.Symbol;
            if (!CouldBeReadOnly(symbol))
                return;

            // 'readonly' cannot be applied to an event accessor, only to the event itself, so the accessors are
            // collected and the event is reported once every one of them can be readonly
            var readOnlyEventAccessors = new ConcurrentDictionary<IEventSymbol, ConcurrentHashSet<IMethodSymbol>>(SymbolEqualityComparer.Default);

            ctx.RegisterOperationBlockAction(ctx =>
            {
                if (!CouldBeReadOnly(ctx.OwningSymbol))
                    return;

                if (ctx.OperationBlocks.Length > 0)
                {
                    if (!EnsureLanguageVersion(ctx.OperationBlocks[0]))
                        return;
                }

                AnalyzeOperationBlock(ctx, readOnlyEventAccessors);
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

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context, ConcurrentDictionary<IEventSymbol, ConcurrentHashSet<IMethodSymbol>> readOnlyEventAccessors)
    {
        foreach (var operationBlock in context.OperationBlocks)
        {
            if (IsInstanceModified(operationBlock))
                return;
        }

        if (context.OwningSymbol is IMethodSymbol method)
        {
            if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet)
            {
                var parent = context.OperationBlocks.FirstOrDefault()?.Syntax.Parent;
                if (parent?.IsKind(SyntaxKind.PropertyDeclaration) == true)
                {
                    // Both parts of a partial property must be readonly, and the code fix only updates the implementation part
                    if (((PropertyDeclarationSyntax)parent).Modifiers.Any(SyntaxKind.PartialKeyword))
                        return;

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

    /// <summary>
    /// The data flow analysis of a block includes the blocks it contains, so only the outermost blocks are analyzed.
    /// </summary>
    private static bool IsInstanceModified(IOperation operation)
    {
        if (operation is not IBlockOperation)
        {
            foreach (var child in operation.GetChildOperations())
            {
                if (IsInstanceModified(child))
                    return true;
            }

            return false;
        }

        var dataFlow = operation.SemanticModel!.AnalyzeDataFlow(GetDataFlowArgument(operation.Syntax));
        foreach (var symbol in dataFlow.WrittenInside)
        {
            if (symbol is IParameterSymbol { IsThis: true })
                return true;
        }

        foreach (var symbol in dataFlow.UnsafeAddressTaken)
        {
            if (symbol is IParameterSymbol { IsThis: true })
                return true;
        }

        return false;
    }

    private static SyntaxNode GetDataFlowArgument(SyntaxNode node)
    {
        if (node is ArrowExpressionClauseSyntax expression)
        {
            return expression.Expression;
        }

        return node;
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

            // Both parts of a partial method must be readonly (CS8663), and the code fix only updates the implementation part
            if (method.PartialDefinitionPart is not null)
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
