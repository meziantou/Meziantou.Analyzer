using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

internal static class InheritdocOnTypesAnalyzerHelper
{
    public static void Analyze(SymbolAnalysisContext context, Func<bool, int, bool> shouldReportDiagnostic, DiagnosticDescriptor rule)
    {
        if (context.Symbol is not INamedTypeSymbol symbol)
            return;

        if (symbol.IsImplicitlyDeclared || symbol.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
            return;

        if (symbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal))
            return;

        var hasBaseType = HasBaseType(symbol);
        var interfaceCount = symbol.Interfaces.Length;
        if (!shouldReportDiagnostic(hasBaseType, interfaceCount))
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
                    if (!IsInheritdocElement(element.Name) || HasCrefAttribute(element.Attributes))
                        continue;

                    context.ReportDiagnostic(rule, element);
                }

                foreach (var element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (!IsInheritdocElement(element.StartTag.Name) || HasCrefAttribute(element.StartTag.Attributes))
                        continue;

                    context.ReportDiagnostic(rule, element.StartTag);
                }
            }
        }
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
