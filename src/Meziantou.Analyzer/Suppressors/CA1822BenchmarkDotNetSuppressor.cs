using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Meziantou.Analyzer.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CA1822BenchmarkDotNetSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor s_rule = new(
        id: "MA_CA1822",
        suppressedDiagnosticId: "CA1822",
        justification: "Suppress CA1822 on methods decorated with BenchmarkDotNet attributes."
    );

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(s_rule);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var attributeSymbol = context.Compilation.GetBestTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");
        if (attributeSymbol == null)
            return;

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            ProcessDiagnostic(context, attributeSymbol, diagnostic);
        }
    }

    private static void ProcessDiagnostic(SuppressionAnalysisContext context, INamedTypeSymbol attributeSymbol, Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        if (location == null)
            return;

        var syntaxTree = location.SourceTree;
        if (syntaxTree == null)
            return;

        var root = syntaxTree.GetRoot(context.CancellationToken);
        var node = root.FindNode(location.SourceSpan);

        var semanticModel = context.GetSemanticModel(syntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
        if (symbol == null)
            return;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeConstructor?.ContainingSymbol.IsEqualTo(attributeSymbol) is true)
            {
                var suppression = Suppression.Create(s_rule, diagnostic);
                context.ReportSuppression(suppression);
                return;
            }
        }
    }
}
