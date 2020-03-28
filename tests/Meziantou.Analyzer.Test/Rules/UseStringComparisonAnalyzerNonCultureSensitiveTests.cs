using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class UseStringComparisonAnalyzerNonCultureSensitiveTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<UseStringComparisonAnalyzer>("MA0001")
                .WithCodeFixProvider<UseStringComparisonFixer>();
        }

        [Fact]
        public async Task Equals_String_string_StringComparison_ShouldNotReportDiagnosticWhenStringComparisonIsSpecifiedAsync()
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

        [Fact]
        public async Task Equals_String_string_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]System.String.Equals(""a"", ""v"");
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
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'Equals' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Equals_String_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        [||]""a"".Equals(""v"");
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
                  .ShouldReportDiagnosticWithMessage("Use an overload of 'Equals' that has a StringComparison parameter")
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task IndexOf_String_StringComparison_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task StartsWith_String_StringComparison_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task Compare_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task IndexOf_ShouldNotReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        """".IndexOf("""", 0, System.StringComparison.Ordinal);
    }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task JObject_Property_ShouldReportDiagnostic()
        {
            const string SourceCode = @"
class TypeName
{
    public void Test()
    {
        var obj = new Newtonsoft.Json.Linq.JObject();
        [||]obj.Property("""");
    }
}

namespace Newtonsoft.Json.Linq
{
    public class JObject
    {
        public void Property(string name) => throw null;
        public void Property(string name, System.StringComparison comparison) => throw null;
    }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
