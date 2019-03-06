using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
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
public class Test
{
    private List<int> a;
    public List<int> b; // Error
    protected List<int> c; // Error
    private protected List<int> d;
    public string e;
}");

            var expected1 = CreateDiagnosticResult(line: 5, column: 5);
            var expected2 = CreateDiagnosticResult(line: 6, column: 5);
            VerifyDiagnostic(project, expected1, expected2);
        }

        [TestMethod]
        public void Delegates()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
internal delegate List<int> A();
public delegate List<int> B(); // Error
public delegate string C();
public delegate void D(object p1);
public delegate void E(List<string> p1); // Error
");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 3, column: 1),
                CreateDiagnosticResult(line: 6, column: 24),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void Indexer()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
public class Test
{
    private List<int> this[int value] => throw null;
    public List<int> this[string value] => throw null; // Error
    protected List<int> this[object value] => throw null; // Error
    private protected List<int> this[short value] => throw null;
    public string this[uint value] => throw null;
    public string this[List<string> value] => throw null; // Error
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 5, column: 5),
                CreateDiagnosticResult(line: 6, column: 5),
                CreateDiagnosticResult(line: 9, column: 24),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void Properties()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
public class Test
{
    private List<int> A => throw null;
    public List<int> B => throw null; // Error
    protected List<int> C => throw null; // Error
    private protected List<int> D => throw null;
    public string E => throw null;
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 5, column: 5),
                CreateDiagnosticResult(line: 6, column: 5),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void Methods()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;
public class Test
{
    private List<int> A() => throw null;
    public List<int> B() => throw null; // Error
    protected List<int> C() => throw null; // Error
    private protected List<int> D() => throw null;
    public string E() => throw null;
    public void F() => throw null;
    public void G(object p1) => throw null;
    public void H(List<int> p1) => throw null;
    internal void I(List<int> p1) => throw null;
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 5, column: 5),
                CreateDiagnosticResult(line: 6, column: 5),
                CreateDiagnosticResult(line: 11, column: 19),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void PrivateContainer()
        {
            var project = new ProjectBuilder()
                 .WithSource(@"using System.Collections.Generic;
internal class Test
{
    public delegate List<int> B();
    public List<int> _a;
    protected List<int> _b;
    public List<int> A() => throw null;
}");

            VerifyDiagnostic(project);
        }
    }
}
