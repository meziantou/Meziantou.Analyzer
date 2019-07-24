using System.Threading.Tasks;
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
        public async Task SingleLineCommentWithoutTodo(string comment)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("//[|]TODO", "")]
        [DataRow("// [|]TODO", "")]
        [DataRow("//[|]TODO test", "test")]
        [DataRow("// [|]TODO test", "test")]
        [DataRow("  // [|]TODO test", "test")]
        public async Task SingleLineComment(string comment, string todo)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ShouldReportDiagnosticWithMessage($"TODO {todo}")
                  .ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("/*[|]TODO*/", "")]
        [DataRow("/* [|]TODO*/", "")]
        [DataRow("/*[|]TODO test*/", "test")]
        [DataRow("/* [|]TODO test*/", "test")]
        [DataRow("  /* [|]TODO test*/", "test")]
        [DataRow("/*\n* [|]TODO test\r\n*/", "test")]
        public async Task MultiLinesComment(string comment, string todo)
        {
            await CreateProjectBuilder()
                  .WithSourceCode(comment)
                  .ShouldReportDiagnosticWithMessage($"TODO {todo}")
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task MultiTodoComment()
        {
            await CreateProjectBuilder()
                  .WithSourceCode(@"
/*
 * [|]TODO a
 * [|]TODO b
 */")
                  .ShouldReportDiagnosticWithMessage("TODO a")
                  .ShouldReportDiagnosticWithMessage("TODO b")
                  .ValidateAsync();
        }
    }
}
