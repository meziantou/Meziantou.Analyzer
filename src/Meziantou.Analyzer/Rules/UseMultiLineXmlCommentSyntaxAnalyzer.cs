using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseMultiLineXmlCommentSyntaxAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseMultiLineXmlCommentSyntax,
        title: "Use multi-line XML comment syntax",
        messageFormat: "Use multi-line XML comment syntax",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "Enforce multi-line XML documentation comment syntax for consistency.",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseMultiLineXmlCommentSyntax));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event, SymbolKind.Property);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared)
            return;

        if (IsCompilerGeneratedType(symbol))
            return;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(context.CancellationToken);
            if (!syntax.HasStructuredTrivia)
                continue;

            foreach (var trivia in syntax.GetLeadingTrivia())
            {
                var structure = trivia.GetStructure();
                if (structure is not DocumentationCommentTriviaSyntax documentation)
                    continue;

                foreach (var childNode in documentation.ChildNodes())
                {
                    if (childNode is not XmlElementSyntax elementSyntax)
                        continue;

                    var startLine = elementSyntax.StartTag.GetLocation().GetLineSpan().StartLinePosition.Line;
                    var endLine = elementSyntax.EndTag.GetLocation().GetLineSpan().EndLinePosition.Line;
                    if (endLine != startLine)
                        continue;

                    var hasCDataOrOtherElements = false;
                    var meaningfulTextTokenCount = 0;
                    foreach (var content in elementSyntax.Content)
                    {
                        if (content is XmlTextSyntax textSyntax)
                        {
                            foreach (var token in textSyntax.TextTokens)
                            {
                                if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.XmlTextLiteralNewLineToken))
                                    continue;

                                var text = token.Text.Trim();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    meaningfulTextTokenCount++;
                                }
                            }
                        }
                        else if (content is XmlCDataSectionSyntax || content is XmlElementSyntax)
                        {
                            hasCDataOrOtherElements = true;
                            break;
                        }
                    }

                    if (!hasCDataOrOtherElements && meaningfulTextTokenCount <= 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, elementSyntax.GetLocation()));
                    }
                }
            }
        }
    }

    private static bool IsCompilerGeneratedType(ISymbol symbol)
    {
        return symbol is INamedTypeSymbol namedTypeSymbol && (namedTypeSymbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal));
    }
}
