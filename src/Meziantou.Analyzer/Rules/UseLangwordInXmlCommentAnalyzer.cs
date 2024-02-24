using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseLangwordInXmlCommentAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
    };

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseLangwordInXmlComment,
        title: "Use langword in XML comment",
        messageFormat: "Use langword in XML comment",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseLangwordInXmlComment));

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

                // Detect the following patterns
                // <c>{keyword}</c>
                // <code>{keyword}</code>
                var queue = new Queue<SyntaxNode>(documentation.ChildNodes());
                while (queue.TryDequeue(out var childNode))
                {
                    if (childNode is XmlElementSyntax elementSyntax)
                    {
                        var elementName = elementSyntax.StartTag.Name.LocalName.Text;
                        if (string.Equals(elementName, "c", StringComparison.OrdinalIgnoreCase) || string.Equals(elementName, "code", StringComparison.OrdinalIgnoreCase))
                        {
                            var item = elementSyntax.Content.SingleOrDefaultIfMultiple();
                            if (item is XmlTextSyntax { TextTokens: [var codeText] } && CSharpKeywords.Contains(codeText.Text))
                            {
                                context.ReportDiagnostic(Rule, elementSyntax);
                            }
                        }
                        else
                        {
                            foreach (var child in elementSyntax.Content)
                            {
                                queue.Enqueue(child);
                            }
                        }
                    }
                }
            }
        }
    }
}
