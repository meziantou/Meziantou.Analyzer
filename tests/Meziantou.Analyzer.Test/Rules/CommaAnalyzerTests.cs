using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class CommaAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<CommaAnalyzer>()
                .WithCodeFixProvider<CommaFixer>();
        }

        [TestMethod]
        public async Task OneLineDeclarationWithMissingTrailingComma_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName() { A = 1 };
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task MultipleLinesDeclarationWithTrailingComma_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task MultipleLinesDeclarationWithMissingTrailingComma_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2
        };
    }
}";
            const string CodeFix = @"
class TypeName
{
    public int A { get; set; }
    public int B { get; set; }

    public async System.Threading.Tasks.Task Test()
    {
        new TypeName()
        {
            A = 1,
            B = 2,
        };
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 12, column: 13, message: "Add comma after the last property")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
