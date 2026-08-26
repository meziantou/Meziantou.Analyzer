using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CA1507SerializationPropertyNameSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor RuleJsonProperty = new(
        id: "MAS0004",
        suppressedDiagnosticId: "CA1507",
        justification: "Suppress CA1507 on methods decorated with a [Newtonsoft.Json.JsonPropertyAttribute]."
    );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(RuleJsonProperty);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var jsonPropertyAttributeSymbol = context.Compilation.GetBestTypeByMetadataName("Newtonsoft.Json.JsonPropertyAttribute");
        if (jsonPropertyAttributeSymbol is null)
            return;

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var node = diagnostic.FindNode(context.CancellationToken);
            if (node is null)
                continue;

            var parent = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (parent is null)
                continue;

            var semanticModel = context.GetSemanticModel(node.SyntaxTree);
            var info = semanticModel.GetSymbolInfo(parent, context.CancellationToken);
            if (info.Symbol is not IMethodSymbol methodSymbol)
                continue;

            if (methodSymbol.ContainingType.IsEqualTo(jsonPropertyAttributeSymbol))
            {
                var suppression = Suppression.Create(RuleJsonProperty, diagnostic);
                context.ReportSuppression(suppression);
            }
        }
    }
}
