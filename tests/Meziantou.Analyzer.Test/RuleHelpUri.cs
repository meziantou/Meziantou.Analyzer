using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Test;
public sealed class RuleHelpUri
{
    [Fact]
    public void HelpUriIsSet()
    {
        foreach (var diag in GetSupportedDiagnostics())
        {
            Assert.Equal($"https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/{diag.Id}.md", diag.HelpLinkUri);
        }
    }

    [Fact]
    public void EachRuleIdHasASingleDescriptor()
    {
        var duplicatedIds = GetSupportedDiagnostics()
            .GroupBy(diag => diag.Id, StringComparer.Ordinal)
            .Where(group => group.DistinctBy(diag => diag.Title.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal).Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Empty(duplicatedIds);
    }

    private static IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
    {
        foreach (var type in typeof(AbstractTypesShouldNotHaveConstructorsAnalyzer).Assembly.GetExportedTypes())
        {
            if (type.IsAbstract)
                continue;

            if (!typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                continue;

            var instance = (DiagnosticAnalyzer)Activator.CreateInstance(type)!;
            foreach (var diag in instance.SupportedDiagnostics)
            {
                yield return diag;
            }
        }
    }
}
