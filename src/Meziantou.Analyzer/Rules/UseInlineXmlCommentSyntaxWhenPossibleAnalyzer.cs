using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseInlineXmlCommentSyntaxWhenPossibleAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseSingleLineXmlCommentSyntaxWhenPossible,
        title: "Use single-line XML comment syntax when possible",
        messageFormat: "Use single-line XML comment syntax when possible",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseSingleLineXmlCommentSyntaxWhenPossible));

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

        if (symbol is INamedTypeSymbol namedTypeSymbol && (namedTypeSymbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal)))
            return;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(context.CancellationToken);
            if (!syntax.HasStructuredTrivia)
                continue;

            foreach (var trivia in syntax.GetLeadingTrivia())
            {
                var structure = trivia.GetStructure();
                if (structure is null)
                    continue;

                if (structure is not DocumentationCommentTriviaSyntax documentation)
                    continue;

                foreach (var childNode in documentation.ChildNodes())
                {
                    if (childNode is XmlElementSyntax elementSyntax)
                    {
                        // Check if element spans multiple lines
                        var startLine = elementSyntax.StartTag.GetLocation().GetLineSpan().StartLinePosition.Line;
                        var endLine = elementSyntax.EndTag.GetLocation().GetLineSpan().EndLinePosition.Line;

                        if (endLine == startLine)
                            continue; // Single line, no issue

                        // Check if content is single-line (ignoring whitespace)
                        // Count the number of text tokens that have meaningful content
                        var meaningfulTextTokenCount = 0;
                        foreach (var content in elementSyntax.Content)
                        {
                            if (content is XmlTextSyntax textSyntax)
                            {
                                foreach (var token in textSyntax.TextTokens)
                                {
                                    // Skip whitespace-only tokens and newline tokens
                                    if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.XmlTextLiteralNewLineToken))
                                        continue;

                                    var text = token.Text.Trim();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        meaningfulTextTokenCount++;
                                    }
                                }
                            }
                        }

                        // Report diagnostic if content is effectively single-line (0 or 1 meaningful text tokens)
                        if (meaningfulTextTokenCount <= 1)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, elementSyntax.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}
