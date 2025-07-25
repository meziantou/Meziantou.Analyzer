using System;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Meziantou.Analyzer.Test;
public sealed class RuleHelpUri
{
    [Fact]
    public void HelpUriIsSet()
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
                Assert.Equal($"https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/{diag.Id}.md", diag.HelpLinkUri);
            }
        }
    }
}
