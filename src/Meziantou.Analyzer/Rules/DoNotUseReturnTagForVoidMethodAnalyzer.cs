using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseReturnTagForVoidMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseReturnTagForVoidMethod,
        title: "Do not use return tag for void method",
        messageFormat: "Do not use return tag for void method",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseReturnTagForVoidMethod));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { IsImplicitlyDeclared: false, ReturnsVoid: true } methodSymbol)
            return;

        if (methodSymbol.MethodKind is not MethodKind.Ordinary and not MethodKind.ExplicitInterfaceImplementation)
            return;

        foreach (var syntaxReference in methodSymbol.DeclaringSyntaxReferences)
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
                    if (IsReturnElement(element.Name))
                    {
                        context.ReportDiagnostic(Rule, element);
                    }
                }

                foreach (var element in documentation.DescendantNodes().OfType<XmlElementSyntax>())
                {
                    if (IsReturnElement(element.StartTag.Name))
                    {
                        context.ReportDiagnostic(Rule, element.StartTag);
                    }
                }
            }
        }
    }

    private static bool IsReturnElement(XmlNameSyntax name)
    {
        return name.LocalName.Text is "returns" or "return";
    }
}
