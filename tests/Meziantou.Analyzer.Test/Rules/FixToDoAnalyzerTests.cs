using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class FixToDoAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<FixToDoAnalyzer>();
        }

        [DataTestMethod]
        [DataRow("//TODOA")]
        [DataRow("// (TODO)")]
        public async System.Threading.Tasks.Task SingleLineCommentWithoutTodoAsync(string comment)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("//TODO", "", 3)]
        [DataRow("// TODO", "", 4)]
        [DataRow("//TODO test", "test", 3)]
        [DataRow("// TODO test", "test", 4)]
        [DataRow("  // TODO test", "test", 6)]
        public async System.Threading.Tasks.Task SingleLineCommentAsync(string comment, string todo, int column)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ShouldReportDiagnostic(line: 1, column: column, message: $"TODO {todo}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("/*TODO*/", "", 1, 3)]
        [DataRow("/* TODO*/", "", 1, 4)]
        [DataRow("/*TODO test*/", "test", 1, 3)]
        [DataRow("/* TODO test*/", "test", 1, 4)]
        [DataRow("  /* TODO test*/", "test", 1, 6)]
        [DataRow("/*\n* TODO test\r\n*/", "test", 2, 3)]
        public async System.Threading.Tasks.Task MultiLinesCommentAsync(string comment, string todo, int line, int column)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ShouldReportDiagnostic(line: line, column: column, message: $"TODO {todo}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task MultiTodoCommentAsync()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
/*
 * TODO a
 * TODO b
 */")
                  .ShouldReportDiagnostic(line: 3, column: 4, message: "TODO a")
                  .ShouldReportDiagnostic(line: 4, column: 4, message: "TODO b")
                  .ValidateAsync();
        }
    }
}
