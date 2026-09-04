#if ROSLYN_5_9_OR_GREATER
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveUnnecessaryClosedModifierAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RemoveUnnecessaryClosedModifier,
        title: "Remove unnecessary closed modifier",
        messageFormat: "Remove unnecessary closed modifier as the type has no derived type",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveUnnecessaryClosedModifier));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolAction(AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

#pragma warning disable RSEXPERIMENTAL006
        if (!symbol.IsClosed)
            return;

        var derivedTypeInfo = symbol.GetClosedDerivedTypeInfo(context.CancellationToken);
#pragma warning restore RSEXPERIMENTAL006
        if (!derivedTypeInfo.IsComplete || !derivedTypeInfo.ClosedDerivedTypes.IsEmpty)
            return;

        // The modifier can be set on a single part of a partial type, so all the declarations must be inspected
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax typeDeclaration)
                continue;

#pragma warning disable RSEXPERIMENTAL006
            var closedToken = typeDeclaration.Modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.ClosedKeyword));
#pragma warning restore RSEXPERIMENTAL006
            if (closedToken == default)
                continue;

            context.ReportDiagnostic(Rule, closedToken.GetLocation());
        }
    }
}
#endif
