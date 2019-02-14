using Meziantou.Analyzer.UsageRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new PreferReturnCollectionAbstractionInsteadOfImplementationAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0016";
        protected override string ExpectedDiagnosticMessage => "Prefer return collection abstraction instead of implementation";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Fields()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
class Test
{
    private List<int> a;
    public List<int> b;
    public string c;
}");

            var expected = CreateDiagnosticResult(line: 5, column: 5);
            VerifyDiagnostic(project, expected);
        }
    }
}
