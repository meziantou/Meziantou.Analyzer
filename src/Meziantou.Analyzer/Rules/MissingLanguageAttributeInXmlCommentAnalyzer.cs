using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

// This rule is hidden by default and reports most of the code elements, so it is not part of UseLangwordInXmlCommentAnalyzer:
// the compiler does not run an analyzer whose rules are all hidden, whereas it would create these diagnostics to discard them.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingLanguageAttributeInXmlCommentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.MissingLanguageAttributeInXmlComment,
        title: "Set the language attribute in XML comment",
        messageFormat: "Set the language attribute in XML comment",
        RuleCategories.Design,
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MissingLanguageAttributeInXmlComment));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        XmlCommentCodeElementHelper.AnalyzeCodeElements(context, AnalyzeCodeElement);
    }

    private static void AnalyzeCodeElement(SyntaxTreeAnalysisContext context, XmlElementSyntax codeElement)
    {
        // MA0154 reports the keywords, as the langword attribute is the way to document them
        if (XmlCommentCodeElementHelper.TryGetKeyword(codeElement, out _))
            return;

        foreach (var attribute in codeElement.StartTag.Attributes)
        {
            if (XmlCommentCodeElementHelper.IsLanguageAttribute(attribute) || XmlCommentCodeElementHelper.IsLangwordAttribute(attribute))
                return;
        }

        context.ReportDiagnostic(Rule, codeElement);
    }
}
