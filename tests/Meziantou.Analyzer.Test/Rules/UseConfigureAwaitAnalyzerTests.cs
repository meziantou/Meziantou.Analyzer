using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseConfigureAwaitAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseConfigureAwaitAnalyzer>()
                .WithCodeFixProvider<UseConfigureAwaitFixer>();
        }

        [Fact]
        public async System.Threading.Tasks.Task MissingConfigureAwait_ShouldReportErrorAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        [||]await Task.Delay(1);
    }
}";
            const string CodeFix = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Delay(1).ConfigureAwait(false);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task ConfigureAwaitIsPresent_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Delay(1).ConfigureAwait(true);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task ConfigureAwaitOfTIsPresent_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Run(() => 10).ConfigureAwait(true);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task MissingConfigureAwaitInWpfWindowClass_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}";
            await CreateProjectBuilder()
                  .AddWpfApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task MissingConfigureAwaitInWpfCommandClass_ShouldNotReportErrorAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class MyClass : System.Windows.Input.ICommand
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}";
            await CreateProjectBuilder()
                  .AddWpfApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task AfterConfigureAwaitFalse_AllAwaitShouldUseConfigureAwaitAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
        await Task.Delay(1).ConfigureAwait(false);
        [||]await Task.Delay(1);
    }
}";
            const string CodeFix = @"using System.Threading.Tasks;
class MyClass : System.Windows.Window
{
    async Task Test()
    {
        await Task.Delay(1);
        await Task.Delay(1).ConfigureAwait(false);
        await Task.Delay(1).ConfigureAwait(false);
    }
}";
            await CreateProjectBuilder()
                  .AddWpfApi()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task AfterConfigureAwaitFalseInANonAccessibleBranch_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
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
}";
            await CreateProjectBuilder()
                  .AddWpfApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task AfterConfigureAwaitFalseInNonAccessibleBranch2_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
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
            [||]await Task.Delay(1);
        }
    }
}";
            await CreateProjectBuilder()
                  .AddWpfApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task TaskYield_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class ClassTest
{
    async Task Test()
    {
        await Task.Yield();
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async System.Threading.Tasks.Task XUnitAttribute_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Threading.Tasks;
class ClassTest
{
    [Xunit.Fact]
    async Task Test()
    {
        await Task.Delay(1);
    }
}";
            await CreateProjectBuilder()
                  .AddXUnitApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
