using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;
using System.Threading.Tasks;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseStringEqualsAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseStringEqualsAnalyzer>()
                .WithCodeFixProvider<UseStringEqualsFixer>();
        }

        [Fact]
        public async Task Equals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [||]""a"" == ""v"";
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        var a = string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnosticWithMessage("Use string.Equals instead of Equals operator")
                .ShouldFixCodeWith(CodeFix)
                .ValidateAsync();
        }

        [Fact]
        public async Task NotEquals_StringLiteral_stringLiteral_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = [||]""a"" != ""v"";
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        var a = !string.Equals(""a"", ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnosticWithMessage("Use string.Equals instead of NotEquals operator")
                .ShouldFixCodeWith(CodeFix)
                .ValidateAsync();
        }

        [Fact]
        public async Task Equals_StringVariable_stringLiteral_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        string str = """";
        var a = [||]str == ""v"";
    }
}";
            const string CodeFix = @"
class TypeName
{
    public void Test()
    {
        string str = """";
        var a = string.Equals(str, ""v"", System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ShouldReportDiagnosticWithMessage("Use string.Equals instead of Equals operator")
                .ShouldFixCodeWith(CodeFix)
                .ValidateAsync();
        }

        [Fact]
        public async Task Equals_ObjectVariable_stringLiteral_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        object str = """";
        var a = str == ""v"";
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }

        [Fact]
        public async Task Equals_stringLiteral_null_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var a = """" == null;
        var b = null == """";
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }

        [Fact]
        public async Task Equals_InIQueryableMethod_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"using System.Linq;
class TypeName
{
    public void Test()
    {
        IQueryable<string> query = null;
        query = query.Where(i => i == ""test"");
    }
}";
            await CreateProjectBuilder()
                .WithSourceCode(SourceCode)
                .ValidateAsync();
        }
    }
}
