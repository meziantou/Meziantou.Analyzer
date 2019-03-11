using System.Collections.Generic;
using System.Linq;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class OptimizeLinqUsageAnalyzerCountTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new OptimizeLinqUsageAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0031";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Info;

        [DataTestMethod]
        [DataRow("Count() == -1", "Expression is always false")]
        [DataRow("Count() == 0", "Replace 'Count() == 0' with 'Any() == false'")]
        [DataRow("Count() == 1", "Replace 'Count() == 1' with 'Any()'")]
        [DataRow("Count() == 2", "Replace 'Count() == 2' with 'Skip(1).Any()'")]
        public void Count_Equals(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }

        [DataTestMethod]
        [DataRow("Count() != -2", "Expression is always true")]
        [DataRow("Count() != 0", "Replace 'Count() != 0' with 'Any()'")]
        public void Count_NotEquals(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }

        [DataTestMethod]
        [DataRow("Count() != 1")]
        [DataRow("Count() != 2")]
        public void Count_NotEquals_Valid(string text)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project);
        }
        
        [DataTestMethod]
        [DataRow("Count() < -1", "Expression is always false")]
        [DataRow("Count() < 0", "Expression is always false")]
        [DataRow("Count() < 1", "Replace 'Count() < 1' with 'Any() == false'")]
        [DataRow("Count() < 2", "Replace 'Count() < 2' with 'Skip(1).Any() == false'")]
        public void Count_LessThan(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }

        [DataTestMethod]
        [DataRow("Count() <= -1", "Expression is always false")]
        [DataRow("Count() <= 0", "Replace 'Count() <= 0' with 'Any() == false'")]
        [DataRow("Count() <= 1", "Replace 'Count() <= 1' with 'Skip(1).Any() == false'")]
        [DataRow("Count() <= 2", "Replace 'Count() <= 2' with 'Skip(2).Any() == false'")]
        public void Count_LessThanOrEqual(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }

        [DataTestMethod]
        [DataRow("Count() > -1", "Expression is always true")]
        [DataRow("Count() > 0", "Replace 'Count() > 0' with 'Any()'")]
        [DataRow("Count() > 1", "Replace 'Count() > 1' with 'Skip(1).Any()'")]
        [DataRow("Count() > 2", "Replace 'Count() > 2' with 'Skip(2).Any()'")]
        public void Count_GreaterThan(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = enumerable." + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }

        [DataTestMethod]
        [DataRow("enumerable.Count() >= -1", "Expression is always true")]
        [DataRow("-1 <= enumerable.Count()", "Expression is always true")]
        [DataRow("enumerable.Count() >= 0", "Expression is always true")]
        [DataRow("enumerable.Count() >= 1", "Replace 'Count() >= 1' with 'Any()'")]
        [DataRow("enumerable.Count() >= 2", "Replace 'Count() >= 2' with 'Skip(1).Any()'")]
        public void Count_GreaterThanOrEqual(string text, string expectedMessage)
        {
            var project = new ProjectBuilder()
                  .AddReference(typeof(IEnumerable<>))
                  .AddReference(typeof(Enumerable))
                  .WithSource(@"using System.Linq;
class Test
{
    public Test()
    {
        var enumerable = System.Linq.Enumerable.Empty<int>();
        _ = " + text + @";
    }
}
");

            VerifyDiagnostic(project, CreateDiagnosticResult(line: 7, column: 13, message: expectedMessage));
        }
    }
}
