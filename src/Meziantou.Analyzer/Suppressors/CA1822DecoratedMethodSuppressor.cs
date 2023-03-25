using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Meziantou.Analyzer.Suppressors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CA1822DecoratedMethodSuppressor : DiagnosticSuppressor
{
    private static readonly SuppressionDescriptor s_ruleBenchmarkDotNet = new(
        id: "MAS0001",
        suppressedDiagnosticId: "CA1822",
        justification: "Suppress CA1822 on methods decorated with BenchmarkDotNet attributes."
    );

    private static readonly SuppressionDescriptor s_ruleJsonPropertyName = new(
        id: "MAS0002",
        suppressedDiagnosticId: "CA1822",
        justification: "Suppress CA1822 on methods decorated with a System.Text.Json attribute such as [JsonPropertyName] or [JsonInclude]."
    );

    private static readonly (SuppressionDescriptor Descriptor, string AttributeName)[] AttributeNames =
    {
        (s_ruleBenchmarkDotNet, "BenchmarkDotNet.Attributes.BenchmarkAttribute"),
        (s_ruleJsonPropertyName, "System.Text.Json.Serialization.JsonPropertyNameAttribute"),
        (s_ruleJsonPropertyName, "System.Text.Json.Serialization.JsonPropertyOrderAttribute"),
        (s_ruleJsonPropertyName, "System.Text.Json.Serialization.JsonRequiredAttribute"),
    };

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(s_ruleBenchmarkDotNet, s_ruleJsonPropertyName);

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var (descriptor, attributeName) in AttributeNames)
        {
            var attributeSymbol = context.Compilation.GetBestTypeByMetadataName(attributeName);
            if (attributeSymbol == null)
                continue;

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                ProcessDiagnostic(context, descriptor, attributeSymbol, diagnostic);
            }
        }
    }

    private static void ProcessDiagnostic(SuppressionAnalysisContext context, SuppressionDescriptor descriptor, INamedTypeSymbol attributeSymbol, Diagnostic diagnostic)
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
                var suppression = Suppression.Create(descriptor, diagnostic);
                context.ReportSuppression(suppression);
                return;
            }
        }
    }
}
