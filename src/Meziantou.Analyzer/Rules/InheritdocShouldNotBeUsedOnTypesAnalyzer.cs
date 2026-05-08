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
        title: "Add dedicated documentation on types",
        messageFormat: "A type should have dedicated documentation instead of '<inheritdoc />'",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldNotBeUsedOnTypes));

    private static readonly DiagnosticDescriptor AmbiguousInheritdocRule = new(
        RuleIdentifiers.InheritdocShouldNotBeAmbiguousOnTypes,
        title: "Specify cref for ambiguous inheritdoc on types",
        messageFormat: "Specify 'cref' for '<inheritdoc />' because this type has multiple declared interfaces and no base type",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldNotBeAmbiguousOnTypes));

    private static readonly DiagnosticDescriptor InheritdocWithoutSourceRule = new(
        RuleIdentifiers.InheritdocShouldHaveSourceOnTypes,
        title: "Do not use inheritdoc on types without inheritance source",
        messageFormat: "Do not use '<inheritdoc />' without 'cref' when this type has no base type and no declared interfaces",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InheritdocShouldHaveSourceOnTypes));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, AmbiguousInheritdocRule, InheritdocWithoutSourceRule);

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

        var hasBaseType = HasBaseType(symbol);
        var interfaceCount = symbol.Interfaces.Length;

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
                    if (!IsInheritdocElement(element.Name) || HasCrefAttribute(element.Attributes))
                        continue;

                    ReportInheritdocDiagnostic(context, element, hasBaseType, interfaceCount);
                }

                foreach (var element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (!IsInheritdocElement(element.StartTag.Name) || HasCrefAttribute(element.StartTag.Attributes))
                        continue;

                    ReportInheritdocDiagnostic(context, element.StartTag, hasBaseType, interfaceCount);
                }
            }
        }
    }

    private static void ReportInheritdocDiagnostic(SymbolAnalysisContext context, SyntaxNode syntaxNode, bool hasBaseType, int interfaceCount)
    {
        if (!hasBaseType)
        {
            if (interfaceCount == 0)
            {
                context.ReportDiagnostic(InheritdocWithoutSourceRule, syntaxNode);
                return;
            }

            if (interfaceCount > 1)
            {
                context.ReportDiagnostic(AmbiguousInheritdocRule, syntaxNode);
                return;
            }
        }

        context.ReportDiagnostic(Rule, syntaxNode);
    }

    private static bool HasBaseType(INamedTypeSymbol symbol)
    {
        return symbol.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) };
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
