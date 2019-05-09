using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class UseStringComparisonAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseStringComparisonAnalyzer>()
                .WithCodeFixProvider<UseStringComparisonFixer>();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecifiedAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        string.Equals(a, ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Equals_String_string_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        System.String.Equals(""a"", ""v"");
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        System.String.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "Use an overload of 'Equals' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Equals_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".Equals(""v"");
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        ""a"".Equals(""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "Use an overload of 'Equals' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexOf_String_StringComparison_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task IndexOf_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"");
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        ""a"".IndexOf(""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "Use an overload of 'IndexOf' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task StartsWith_String_StringComparison_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task StartsWith_String_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"");
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        ""a"".StartsWith(""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "Use an overload of 'StartsWith' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Compare_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        string.Compare(""a"", ""v"");
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        string.Compare(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 6, column: 9, message: "Use an overload of 'Compare' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Compare_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        string.Compare(""a"", ""v"", ignoreCase: true);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
