using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class FixToDoAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new FixToDoAnalyzer();
        protected override string ExpectedDiagnosticId => "MA0026";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;

        [DataTestMethod]
        [DataRow("//TODOA")]
        [DataRow("// (TODO)")]
        public void SingleLineCommentWithoutTodo(string comment)
        {
            var project = new ProjectBuilder()
                  .WithSource(comment);

            VerifyDiagnostic(project);
        }

        [DataTestMethod]
        [DataRow("//TODO", "", 3)]
        [DataRow("// TODO", "", 4)]
        [DataRow("//TODO test", "test", 3)]
        [DataRow("// TODO test", "test", 4)]
        [DataRow("  // TODO test", "test", 6)]
        public void SingleLineComment(string comment, string todo, int column)
        {
            var project = new ProjectBuilder()
                  .WithSource(comment);

            var expected = new[]
            {
                CreateDiagnosticResult(line: 1, column: column, message: $"TODO {todo}"),
            };

            VerifyDiagnostic(project, expected);
        }

        [DataTestMethod]
        [DataRow("/*TODO*/", "", 1, 3)]
        [DataRow("/* TODO*/", "", 1, 4)]
        [DataRow("/*TODO test*/", "test", 1, 3)]
        [DataRow("/* TODO test*/", "test", 1, 4)]
        [DataRow("  /* TODO test*/", "test", 1, 6)]
        [DataRow("/*\n* TODO test\r\n*/", "test", 2, 3)]
        public void MultiLinesComment(string comment, string todo, int line, int column)
        {
            var project = new ProjectBuilder()
                  .WithSource(comment);

            var expected = new[]
            {
                CreateDiagnosticResult(line: line, column: column, message: $"TODO {todo}"),
            };

            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void MultiTodoComment()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"
/*
 * TODO a
 * TODO b
 */");

            var expected = new[]
            {
                CreateDiagnosticResult(line: 3, column: 4, message: "TODO a"),
                CreateDiagnosticResult(line: 4, column: 4, message: "TODO b"),
            };

            VerifyDiagnostic(project, expected);
        }
    }
}
