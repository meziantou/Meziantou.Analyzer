using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InheritdocShouldBeUsedOnInheritingMemberAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.InheritdocShouldBeUsedOnInheritingMember,
        title: "Do not use inheritdoc on non-inheriting members",
        messageFormat: "Do not use '<inheritdoc />' on members that are not overrides or interface implementations",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldBeUsedOnInheritingMember));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method, SymbolKind.Property, SymbolKind.Event);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not (IMethodSymbol or IPropertySymbol or IEventSymbol))
            return;

        if (context.Symbol.IsImplicitlyDeclared || context.Symbol.IsOverrideOrInterfaceImplementation())
            return;

        foreach (var syntaxReference in context.Symbol.DeclaringSyntaxReferences)
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
                    if (!IsInheritdocElement(element.Name) || HasCrefAttribute(element.Attributes))
                        continue;

                    context.ReportDiagnostic(Rule, element);
                }

                foreach (var element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (!IsInheritdocElement(element.StartTag.Name) || HasCrefAttribute(element.StartTag.Attributes))
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

    private static bool HasCrefAttribute(SyntaxList<XmlAttributeSyntax> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (string.Equals(attribute.Name.LocalName.Text, "cref", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
