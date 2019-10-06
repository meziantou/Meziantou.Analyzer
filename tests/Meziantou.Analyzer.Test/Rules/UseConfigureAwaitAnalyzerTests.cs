﻿using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;
using System.Threading.Tasks;

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
        public async Task MissingConfigureAwait_ShouldReportError()
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
        public async Task ConfigureAwaitIsPresent_ShouldNotReportError()
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
        public async Task ConfigureAwaitOfTIsPresent_ShouldNotReportError()
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
        public async Task MissingConfigureAwaitInWpfWindowClass_ShouldNotReportError()
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
        public async Task MissingConfigureAwaitInWpfCommandClass_ShouldNotReportError()
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
        public async Task AfterConfigureAwaitFalse_AllAwaitShouldUseConfigureAwait()
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
        public async Task AfterConfigureAwaitFalseInANonAccessibleBranch_ShouldNotReportDiagnostic()
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
        public async Task AfterConfigureAwaitFalseInNonAccessibleBranch2_ShouldReportDiagnostic()
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
        public async Task TaskYield_ShouldNotReportDiagnostic()
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
        public async Task XUnitAttribute_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task Blazor_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"using System.Threading.Tasks;
namespace Microsoft.AspNetCore.Components
{
    public interface IComponent
    {
    }
}

class ClassTest : Microsoft.AspNetCore.Components.IComponent
{
    async Task Test()
    {
        await Task.Delay(1);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
