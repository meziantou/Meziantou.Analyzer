using System.Linq;
using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class MethodShouldNotBeTooLongAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MethodShouldNotBeTooLongAnalyzer>();
        }

        [TestMethod]
        public async Task TooLongMethod()
        {
            const string SourceCode = @"
public class Test
{
    void [|]Method()
    {
        var a = 0;
        var b = 0;
        var c = 0;
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .WithEditorConfig("meziantou.maximumStatementsPerMethod=2")
                  .ValidateAsync();
        }

        [TestMethod]
        public void CountStatement_ForLoop()
        {
            const string SourceCode = @"
for (int a = 0; i < 0; i++)
{
    throw null;
}";

            var count = CountStatements(SourceCode);
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void CountStatement_LocalFunction()
        {
            const string SourceCode = @"
throw null;

void B()
{
    throw null;
}
";

            var count = CountStatements(SourceCode);
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void CountStatement_If()
        {
            const string SourceCode = @"
if (true)
{
    throw null;
}
";

            var count = CountStatements(SourceCode);
            Assert.AreEqual(2, count);
        }

        private static int CountStatements(string code)
        {
            var tree = CSharpSyntaxTree.ParseText("void A(){ " + code + " }");

            var root = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().First();
            return MethodShouldNotBeTooLongAnalyzer.CountStatements(root);
        }
    }
}
