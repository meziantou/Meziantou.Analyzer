using Meziantou.Analyzer.UsageRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.UsageRules
{
    [TestClass]
    public class UseConfigureAwaitAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UseConfigureAwaitAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UseConfigureAwaitFixer();
        protected override string ExpectedDiagnosticId => "MA0004";
        protected override DiagnosticSeverity ExpectedDiagnosticSeverity => DiagnosticSeverity.Warning;
        protected override string ExpectedDiagnosticMessage => "Use ConfigureAwait(false) as the current SynchronizationContext is not needed";

        [TestMethod]
        public void EmptyString_ShouldNotReportDiagnosticForEmptyString()
        {
            var project = new ProjectBuilder();
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MissingConfigureAwait_ShouldReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}");

            var expected = CreateDiagnosticResult(line: 6, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Delay(1).ConfigureAwait(false);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void ConfigureAwaitIsPresent_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Delay(1).ConfigureAwait(true);
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void ConfigureAwaitOfTIsPresent_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Run(() => 10).ConfigureAwait(true);
    }
}");
            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MissingConfigureAwaitInWpfWindowClass_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddWpfApi()
                  .WithSource(@"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void MissingConfigureAwaitInWpfCommandClass_ShouldNotReportError()
        {
            var project = new ProjectBuilder()
                  .AddWpfApi()
                  .WithSource(@"using System.Threading.Tasks;
class MyClass : System.Windows.Input.ICommand
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AfterConfigureAwaitFalse_AllAwaitShouldUseConfigureAwait()
        {
            var project = new ProjectBuilder()
                  .AddWpfApi()
                  .WithSource(@"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
        await Task.Delay(1).ConfigureAwait(false);
        await Task.Delay(1);
    }
}");

            var expected = CreateDiagnosticResult(line: 8, column: 9);
            VerifyDiagnostic(project, expected);

            var fixtest = @"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
        await Task.Delay(1).ConfigureAwait(false);
        await Task.Delay(1).ConfigureAwait(false);
    }
}";
            VerifyFix(project, fixtest);
        }

        [TestMethod]
        public void AfterConfigureAwaitFalseInANonAccessibleBranch_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddWpfApi()
                  .WithSource(@"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        bool a = true;
        if (a)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return;
        }

        await Task.Delay(1);
    }
}");

            VerifyDiagnostic(project);
        }

        [TestMethod]
        public void AfterConfigureAwaitFalseInNonAccessibleBranch2_ShouldReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .AddWpfApi()
                  .WithSource(@"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        bool a = true;
        if (a)
        {
            await Task.Delay(1).ConfigureAwait(false);
        }
        else
        {
            await Task.Delay(1);
        }
    }
}");

            var expected = CreateDiagnosticResult(line: 13, column: 13);
            VerifyDiagnostic(project, expected);
        }

        [TestMethod]
        public void TaskYield_ShouldNotReportDiagnostic()
        {
            var project = new ProjectBuilder()
                  .WithSource(@"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Yield();
    }
}");

            VerifyDiagnostic(project);
        }
    }
}
