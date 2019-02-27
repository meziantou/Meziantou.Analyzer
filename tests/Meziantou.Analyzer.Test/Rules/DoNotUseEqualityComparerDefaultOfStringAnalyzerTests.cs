using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseEqualityComparerDefaultOfStringAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotUseEqualityComparerDefaultOfStringAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0024";
        protected override string ExpectedDiagnosticMessage => "Use StringComparer.Ordinal";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Test()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
class Test
{
    internal void Sample()
    {
        _ = EqualityComparer<int>.Default.Equals(0, 0);
        _ = EqualityComparer<string>.Default.Equals(null, null);
    }
}
");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 7, column: 13),
            };
            VerifyDiagnostic(project, expected);
        }
    }
}
