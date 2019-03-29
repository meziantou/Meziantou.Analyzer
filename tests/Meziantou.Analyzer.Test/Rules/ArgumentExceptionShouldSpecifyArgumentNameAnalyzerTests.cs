using System.ComponentModel;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class ArgumentExceptionShouldSpecifyArgumentNameAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ArgumentExceptionShouldSpecifyArgumentNameAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0015";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void ArgumentNameIsSpecified_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(InvalidEnumArgumentException))
                  .WithSourceCode(@"
class Sample
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(nameof(value)); }
    }

    string this[int index]
    {
        get { throw new System.ArgumentNullException(nameof(index)); }
        set { throw new System.ArgumentNullException(nameof(index)); }
    }

    Sample(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", ""test"");
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
    }

    void Test(string test)
    {
        throw new System.Exception();
        throw new System.ArgumentException(""message"", ""test"");
        throw new System.ArgumentException(""message"", nameof(test));
        throw new System.ArgumentNullException(nameof(test));
        throw new System.ComponentModel.InvalidEnumArgumentException(nameof(test), 0, typeof(System.Enum));

        void LocalFunction(string a)
        {
            throw new System.ArgumentNullException(nameof(a));
        }
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ArgumentNameDoesNotMatchAParameter_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(""unknown""); }
    }

    void Test(string test)
    {
        throw new System.ArgumentException(""message"", ""unknown"");
    }    
}");

            var expected1 = CreateDiagnosticResult(line: 7, column: 21, message: "'unknown' is not a valid parameter name");
            var expected2 = CreateDiagnosticResult(line: 12, column: 15, message: "'unknown' is not a valid parameter name");
            VerifyDiagnostic(project, expected1, expected2);
        }

        [TestMethod]
        public void OverloadWithoutParameterName_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSourceCode(@"
class TestAttribute
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(); }
    }

    void Test(string test)
    {
        throw new System.ArgumentException(""message"");
    }    
}");

            var expected1 = CreateDiagnosticResult(line: 7, column: 21, message: "Use an overload of 'System.ArgumentNullException' with the parameter name");
            var expected2 = CreateDiagnosticResult(line: 12, column: 15, message: "Use an overload of 'System.ArgumentException' with the parameter name");
            VerifyDiagnostic(project, expected1, expected2);
        }
    }
}
