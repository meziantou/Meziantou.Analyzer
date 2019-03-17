using System.Collections.Generic;
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

        private static IEnumerable<object[]> ReturnTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "Task<List<int>>", false };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        private static IEnumerable<object[]> ParametersTypeValues
        {
            get
            {
                yield return new object[] { "private", "List<int>", true };
                yield return new object[] { "public", "List<int>", false };
                yield return new object[] { "protected", "List<int>", false };
                yield return new object[] { "private protected", "List<int>", true };
                yield return new object[] { "public", "string", true };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public void Fields(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" _a;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public void Delegates(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate
    " + type + @" A();
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public void Delegates_Parameters(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" delegate void A(
    " + type + @" p);
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public void Indexers(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" this[int value] => throw null;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public void Indexers_Parameters(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" int this[
    " + type + @" value] => throw null;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public void Properties(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" A => throw null;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ReturnTypeValues), DynamicDataSourceType.Property)]
        public void Methods(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @"
    " + type + @" A() => throw null;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(ParametersTypeValues), DynamicDataSourceType.Property)]
        public void Methods_Parameters(string visibility, string type, bool isValid)
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Collections.Generic;using System.Threading.Tasks;
public class Test
{
    " + visibility + @" void A(
    " + type + @" p) => throw null;
}");

            if (isValid)
            {
                VerifyDiagnostic(project);
            }
            else
            {
                VerifyDiagnostic(project, CreateDiagnosticResult(line: 5, column: 5));
            }
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
