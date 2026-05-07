using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InheritdocShouldNotBeUsedOnTypesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InheritdocShouldNotBeUsedOnTypes,
        title: "Do not use inheritdoc on types",
        messageFormat: "Do not use '<inheritdoc />' on types; use it on members only",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldNotBeUsedOnTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol symbol)
            return;

        if (symbol.IsImplicitlyDeclared || symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return;

        if (symbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal))
            return;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(context.CancellationToken);
            if (!syntax.HasStructuredTrivia)
                continue;

            foreach (var trivia in syntax.GetLeadingTrivia())
            {
                if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax documentation)
                    continue;

                foreach (var element in documentation.DescendantNodes().OfType<XmlEmptyElementSyntax>())
                {
                    if (!IsInheritdocElement(element.Name))
                        continue;

                    context.ReportDiagnostic(Rule, element);
                }

                foreach (var element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (!IsInheritdocElement(element.StartTag.Name))
                        continue;

                    context.ReportDiagnostic(Rule, element.StartTag);
                }
            }
        }
    }

    private static bool IsInheritdocElement(XmlNameSyntax name)
    {
        return string.Equals(name.LocalName.Text, "inheritdoc", StringComparison.OrdinalIgnoreCase);
    }
}
