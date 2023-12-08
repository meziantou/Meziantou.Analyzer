using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CA1822DecoratedMethodSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor RuleBenchmarkDotNet = new(
        id: "MAS0001",
        suppressedDiagnosticId: "CA1822",
        justification: "Suppress CA1822 on methods decorated with BenchmarkDotNet attributes."
    );

    private static readonly SuppressionDescriptor RuleJsonPropertyName = new(
        id: "MAS0002",
        suppressedDiagnosticId: "CA1822",
        justification: "Suppress CA1822 on methods decorated with a System.Text.Json attribute such as [JsonPropertyName] or [JsonInclude]."
    );

    private static readonly (SuppressionDescriptor Descriptor, string AttributeName)[] AttributeNames =
    [
        (RuleBenchmarkDotNet, "BenchmarkDotNet.Attributes.BenchmarkAttribute"),
        (RuleJsonPropertyName, "System.Text.Json.Serialization.JsonPropertyNameAttribute"),
        (RuleJsonPropertyName, "System.Text.Json.Serialization.JsonPropertyOrderAttribute"),
        (RuleJsonPropertyName, "System.Text.Json.Serialization.JsonRequiredAttribute"),
    ];

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(RuleBenchmarkDotNet, RuleJsonPropertyName);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var (descriptor, attributeName) in AttributeNames)
        {
            var attributeSymbol = context.Compilation.GetBestTypeByMetadataName(attributeName);
            if (attributeSymbol is null)
                continue;

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                ProcessDiagnostic(context, descriptor, attributeSymbol, diagnostic);
            }
        }
    }

    private static void ProcessDiagnostic(SuppressionAnalysisContext context, SuppressionDescriptor descriptor, INamedTypeSymbol attributeSymbol, Diagnostic diagnostic)
    {
        var node = diagnostic.TryFindNode(context.CancellationToken);
        if (node is null)
            return;

        var semanticModel = context.GetSemanticModel(node.SyntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
        if (symbol is null)
            return;

        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeConstructor?.ContainingSymbol.IsEqualTo(attributeSymbol) is true)
            {
                var suppression = Suppression.Create(descriptor, diagnostic);
                context.ReportSuppression(suppression);
                return;
            }
        }
    }
}
