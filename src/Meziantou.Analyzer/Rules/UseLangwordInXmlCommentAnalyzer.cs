using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UseLangwordInXmlCommentAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.UseLangwordInXmlComment,
        title: "Use langword in XML comment",
        messageFormat: "Use langword in XML comment",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.UseLangwordInXmlComment));

    private static readonly DiagnosticDescriptor EmptyLanguageAttributeRule = new(
        RuleIdentifiers.EmptyLanguageAttributeInXmlComment,
        title: "The language attribute is empty",
        messageFormat: "The '{0}' attribute is empty",
        RuleCategories.Design,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.EmptyLanguageAttributeInXmlComment));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule, EmptyLanguageAttributeRule);

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
        if (XmlCommentCodeElementHelper.TryGetKeyword(codeElement, out var keyword))
        {
            // The langword attribute is the way to document a keyword, so the language is not needed
            var properties = ImmutableDictionary<string, string?>.Empty.Add(UseLangwordInXmlCommentAnalyzerCommon.KeywordKey, keyword);
            context.ReportDiagnostic(Rule, properties, codeElement);
            return;
        }

        foreach (var attribute in codeElement.StartTag.Attributes)
        {
            if (!XmlCommentCodeElementHelper.IsLanguageAttribute(attribute))
                continue;

            if (attribute is XmlTextAttributeSyntax textAttribute && textAttribute.TextTokens.All(token => string.IsNullOrWhiteSpace(token.ValueText)))
            {
                context.ReportDiagnostic(EmptyLanguageAttributeRule, attribute, attribute.Name.LocalName.Text);
            }
        }
    }
}
