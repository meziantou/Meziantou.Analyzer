using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;
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
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            var node = diagnostic.TryFindNode(context.CancellationToken);
            if (node is null)
                return;

            var parent = node.FirstAncestorOrSelf<AttributeSyntax>();
            if (parent is null)
                return;

            var semanticModel = context.GetSemanticModel(node.SyntaxTree);
            var info = semanticModel.GetSymbolInfo(parent, context.CancellationToken);
            if (info.Symbol is not IMethodSymbol methodSymbol)
                return;

            if (methodSymbol.ContainingType.IsEqualTo(context.Compilation.GetBestTypeByMetadataName("Newtonsoft.Json.JsonPropertyAttribute")))
            {
                var suppression = Suppression.Create(RuleJsonProperty, diagnostic);
                context.ReportSuppression(suppression);
            }
        }
    }
}
