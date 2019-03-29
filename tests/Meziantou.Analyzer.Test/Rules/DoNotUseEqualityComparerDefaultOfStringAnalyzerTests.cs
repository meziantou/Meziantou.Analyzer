using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotUseEqualityComparerDefaultOfStringAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new DoNotUseEqualityComparerDefaultOfStringAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new DoNotUseEqualityComparerDefaultOfStringFixer();
        protected override string ExpectedDiagnosticId => "MA0024";
        protected override string ExpectedDiagnosticMessage => "Use StringComparer.Ordinal";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Test()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"using System.Collections.Generic;
class Test
{
    internal void Sample()
    {
        _ = EqualityComparer<int>.Default.Equals(0, 0);
        _ = EqualityComparer<string>.Default.Equals(null, null);
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13));
            VerifyFix(project, @"using System.Collections.Generic;
class Test
{
    internal void Sample()
    {
        _ = EqualityComparer<int>.Default.Equals(0, 0);
        _ = System.StringComparer.Ordinal.Equals(null, null);
    }
}
");
        }
    }
}
