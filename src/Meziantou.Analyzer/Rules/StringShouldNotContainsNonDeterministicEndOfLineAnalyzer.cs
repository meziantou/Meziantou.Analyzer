using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringShouldNotContainsNonDeterministicEndOfLineAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.StringShouldNotContainsNonDeterministicEndOfLine,
        title: "String contains an implicit end of line character",
        messageFormat: "String contains an implicit end of line character",
        RuleCategories.Usage,
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.StringShouldNotContainsNonDeterministicEndOfLine));

    private static readonly DiagnosticDescriptor RuleRawString = new(
        RuleIdentifiers.RawStringShouldNotContainsNonDeterministicEndOfLine,
        title: "Raw String contains an implicit end of line character",
        messageFormat: "Raw String contains an implicit end of line character",
        RuleCategories.Usage,
        DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.RawStringShouldNotContainsNonDeterministicEndOfLine));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, RuleRawString);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeStringLiteralExpression, SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteralExpression, SyntaxKind.Utf8StringLiteralExpression);
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var node = (InterpolatedStringExpressionSyntax)context.Node;
        var isRawString = node.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken);

        foreach (var item in node.Contents)
        {
            if (item is InterpolatedStringTextSyntax text)
            {
                var position = text.GetLocation().GetLineSpan();
                if (position.StartLinePosition.Line != position.EndLinePosition.Line)
                {
                    context.ReportDiagnostic(isRawString ? RuleRawString : Rule, node);
                    return;
                }
            }
        }
    }

    private static void AnalyzeStringLiteralExpression(SyntaxNodeAnalysisContext context)
    {
        var node = (LiteralExpressionSyntax)context.Node;
        if (node.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken))
            return;

        var position = node.GetLocation().GetLineSpan();
        var startLine = position.StartLinePosition.Line;
        var endLine = position.EndLinePosition.Line;

        var isRawString = node.Token.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken) || node.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken);
        if (isRawString)
        {
            startLine++;
            endLine--;
        }

        if (startLine != endLine)
        {
            context.ReportDiagnostic(isRawString ? RuleRawString : Rule, node);
        }
    }
}
