using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RemoveUnnecessaryPartialModifierAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.RemoveUnnecessaryPartialModifier,
        title: "Remove unnecessary partial modifier",
        messageFormat: "Remove unnecessary partial modifier",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RemoveUnnecessaryPartialModifier));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

        context.RegisterCompilationStartAction(context =>
        {
            var wpfUserControlSymbol = context.Compilation.GetBestTypeByMetadataName("System.Windows.Controls.UserControl");
            var wpfPageSymbol = context.Compilation.GetBestTypeByMetadataName("System.Windows.Controls.Page");
            var wpfWindowSymbol = context.Compilation.GetBestTypeByMetadataName("System.Windows.Window");
            var wpfApplicationSymbol = context.Compilation.GetBestTypeByMetadataName("System.Windows.Application");

            context.RegisterSymbolAction(context => AnalyzeNamedTypeSymbol(context, wpfUserControlSymbol, wpfPageSymbol, wpfWindowSymbol, wpfApplicationSymbol), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context, INamedTypeSymbol? wpfUserControlSymbol, INamedTypeSymbol? wpfPageSymbol, INamedTypeSymbol? wpfWindowSymbol, INamedTypeSymbol? wpfApplicationSymbol)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return;

        if (symbol.DeclaringSyntaxReferences.Length != 1)
            return;

        var typeDeclaration = symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as TypeDeclarationSyntax;
        if (typeDeclaration is null)
            return;

        var partialToken = typeDeclaration.Modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (partialToken == default)
            return;

        if (InheritsFromWpfXamlType(symbol, wpfUserControlSymbol, wpfPageSymbol, wpfWindowSymbol, wpfApplicationSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, partialToken.GetLocation()));
    }

    private static bool InheritsFromWpfXamlType(INamedTypeSymbol symbol, INamedTypeSymbol? wpfUserControlSymbol, INamedTypeSymbol? wpfPageSymbol, INamedTypeSymbol? wpfWindowSymbol, INamedTypeSymbol? wpfApplicationSymbol)
    {
        return symbol.InheritsFrom(wpfUserControlSymbol) ||
               symbol.InheritsFrom(wpfPageSymbol) ||
               symbol.InheritsFrom(wpfWindowSymbol) ||
               symbol.InheritsFrom(wpfApplicationSymbol);
    }
}
