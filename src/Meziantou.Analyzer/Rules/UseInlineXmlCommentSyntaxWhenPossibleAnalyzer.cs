using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseInlineXmlCommentSyntaxWhenPossibleAnalyzer : DiagnosticAnalyzer
{
    private static readonly ConfigurationDefinition<string> MaxLineLengthConfiguration = new("max_line_length");

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
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

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
            var syntaxToAnalyze = syntax switch
            {
                VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax fieldDeclaration } => fieldDeclaration,
                _ => syntax,
            };

            if (!syntaxToAnalyze.HasStructuredTrivia)
                continue;

            foreach (var trivia in syntaxToAnalyze.GetLeadingTrivia())
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

                        // The content must fit on a single line, and the code fixer must be able to rewrite it
                        var inlineElement = UseInlineXmlCommentSyntaxWhenPossibleCommon.CreateInlineElement(elementSyntax);
                        if (inlineElement is null)
                            continue;

                        // Check if the single-line version would fit within max_line_length
                        if (WouldFitInMaxLineLength(context, elementSyntax, inlineElement))
                        {
                            context.ReportDiagnostic(Rule, elementSyntax.GetLocation());
                        }
                    }
                }
            }
        }
    }

    private static bool WouldFitInMaxLineLength(SymbolAnalysisContext context, XmlElementSyntax elementSyntax, XmlElementSyntax inlineElement)
    {
        // Get max_line_length from .editorconfig
        if (!context.Options.TryGetConfigurationValue(elementSyntax.SyntaxTree, MaxLineLengthConfiguration, out var maxLineLengthValue))
            return true; // No limit configured, allow the change

        if (!int.TryParse(maxLineLengthValue, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var maxLineLength) || maxLineLength <= 0)
            return true; // Invalid or no limit, allow the change

        // Get the indentation of the current line
        var lineSpan = elementSyntax.GetLocation().GetLineSpan();
        var sourceText = elementSyntax.SyntaxTree.GetText();
        var line = sourceText.Lines[lineSpan.StartLinePosition.Line];
        var lineText = line.ToString();
        var indentation = lineText.Length - lineText.TrimStart().Length;

        // The resulting line is the indentation, the "/// " prefix, and the element written on a single line
        return indentation + 4 + inlineElement.ToFullString().Length <= maxLineLength;
    }
}
