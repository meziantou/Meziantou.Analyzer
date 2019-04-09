using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class ArgumentExceptionShouldSpecifyArgumentNameAnalyzer_UseNameofTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<ArgumentExceptionShouldSpecifyArgumentNameAnalyzer>(id: "MA0043")
                .WithCodeFixProvider<ArgumentExceptionShouldSpecifyArgumentNameFixer>();
        }

        [TestMethod]
        public async Task Property()
        {
            const string SourceCode = @"
class Sample
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(""value""); }
    }
}";

            const string CodeFix = @"
class Sample
{
    string Prop
    {
        get { throw null; }
        set { throw new System.ArgumentNullException(nameof(value)); }
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 7, column: 54)
                  .ShouldFixCodeWith(CodeFix)
                  .CodeFixAllowNewCompilerDiagnostics() // TODO remove
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task Method()
        {
            const string SourceCode = @"
class Sample
{
    string M(string arg0)
    {
        throw new System.ArgumentNullException(""arg0"");
    }
}";

            const string CodeFix = @"
class Sample
{
    string M(string arg0)
    {
        throw new System.ArgumentNullException(nameof(arg0));
    }
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 48)
                  .ShouldFixCodeWith(CodeFix)
                  .CodeFixAllowNewCompilerDiagnostics() // TODO remove
                  .ValidateAsync();
        }
    }
}
