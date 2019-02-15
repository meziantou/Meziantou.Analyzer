using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class AbstractTypesShouldNotHaveConstructorsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new AbstractTypesShouldNotHaveConstructorsAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0017";
        protected override string ExpectedDiagnosticMessage => "Abstract types should not have public or internal constructors";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Ctor()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
abstract class Test
{
    public Test() { } // Error
    internal Test(long a) { } // Error
    protected Test(int a) { }
    private Test(object a) { }
}

class Test2
{
    public Test2() { }
    internal Test2(long a) { }
    protected Test2(int a) { }
    private Test2(object a) { }
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 4, column: 5),
                CreateDiagnosticResult(line: 5, column: 5),
            };
            VerifyDiagnostic(project, expected);
        }

    }
}
