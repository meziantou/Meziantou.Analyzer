using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class ReturnTaskFromResultInsteadOfReturningNullAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new ReturnTaskFromResultInsteadOfReturningNullAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0022";
        protected override string ExpectedDiagnosticMessage => "Return Task.FromResult instead of returning null";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [TestMethod]
        public void Method()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class Test
{
    Task A() { return null; }
    Task B() => null;
    Task C() { return ((Test)null)?.A(); }
    async Task<object> Valid() { return null; }
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 4, column: 16),
                CreateDiagnosticResult(line: 5, column: 17),
                CreateDiagnosticResult(line: 6, column: 16),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void LocalFunction()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class Test
{
    void A()
    {
        Task<object> Valid1() { return Task.FromResult<object>(null); }
        async Task<object> Valid2() { return null; }
        Task A() { return null; }
        Task<object> B() { return null; }
        Task<object> C() => null;
        object       D() => null;
    }
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 8, column: 20),
                CreateDiagnosticResult(line: 9, column: 28),
                CreateDiagnosticResult(line: 10, column: 29),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void LambdaExpression()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task>         a = () => null;
        System.Func<Task<object>> b = () => null;
        System.Func<Task<object>> c = () => { return null; };
        System.Func<Task>         valid1 = async () => { };
        System.Func<Task<object>> valid2 = async () => null;
        System.Func<object>       valid3 = () => null;
    }
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 6, column: 45),
                CreateDiagnosticResult(line: 7, column: 45),
                CreateDiagnosticResult(line: 8, column: 47),
            };
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void AnonymousMethods()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class Test
{
    void A()
    {
        System.Func<Task> a = delegate () { return null; };
        System.Func<Task<object>> b = delegate () { return null; };
        System.Func<Task> c = async delegate () { };
        System.Func<Task<object>> d = async delegate () { return null; };
        System.Func<object> e = delegate () { return null; };
    }
}");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 6, column: 45),
                CreateDiagnosticResult(line: 7, column: 53),
            };
            VerifyDiagnostic(project, expected);
        }

        //        [TestMethod]
        //        public void Properties()
        //        {
        //            var project = new ProjectBuilder()
        //                  .WithSource(@"using System.Threading.Tasks;
        //class Test
        //{
        //    Task A { get { return null; } }
        //    Task B => null;
        //    Task<object> B => null;
        //    object Valid => null;
        //    object Valid { get { return null; } }
        //}");

        //            var expected = new[]
        //            {
        //                CreateDiagnosticResult(line: 4, column: 20),
        //                CreateDiagnosticResult(line: 5, column: 21),
        //                CreateDiagnosticResult(line: 6, column: 23),
        //            };
        //            VerifyDiagnostic(project, expected);
        //        }
    }
}
