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
        // Resolved on the first diagnostic located in an attribute, so the symbol is not loaded when there is none
        INamedTypeSymbol? jsonPropertyAttributeSymbol = null;
        var jsonPropertyAttributeSymbolResolved = false;

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

            if (!jsonPropertyAttributeSymbolResolved)
            {
                jsonPropertyAttributeSymbol = context.Compilation.GetBestTypeByMetadataName("Newtonsoft.Json.JsonPropertyAttribute");
                jsonPropertyAttributeSymbolResolved = true;
            }

            if (jsonPropertyAttributeSymbol is null)
                return;

            if (methodSymbol.ContainingType.IsEqualTo(jsonPropertyAttributeSymbol))
            {
                var suppression = Suppression.Create(RuleJsonProperty, diagnostic);
                context.ReportSuppression(suppression);
            }
        }
    }
}
