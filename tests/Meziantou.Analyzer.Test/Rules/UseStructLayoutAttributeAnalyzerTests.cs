using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseStructLayoutAttributeAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseStructLayoutAttributeAnalyzer>()
                .WithCodeFixProvider<UseStructLayoutAttributeFixer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task MissingAttribute_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = "struct TypeName { }";
            const string CodeFix = @"[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
struct TypeName { }";

            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnostic(line: 1, column: 8)
                .ShouldFixCodeWith(CodeFix)
                .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task AddAttributeShouldUseShortnameAsync()
        {
            const string SourceCode = @"using System.Runtime.InteropServices;
struct TypeName { }";
            const string CodeFix = @"using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
struct TypeName { }";

            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnostic(line: 2, column: 8)
                .ShouldFixCodeWith(CodeFix)
                .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task WithAttribute_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"using System.Runtime.InteropServices;
[StructLayout(LayoutKind.Sequential)]
struct TypeName
{
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Enum_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
enum TypeName
{
    None,
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }
    }
}
