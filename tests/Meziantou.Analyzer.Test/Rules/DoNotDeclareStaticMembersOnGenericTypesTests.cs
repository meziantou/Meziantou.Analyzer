using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public class DoNotDeclareStaticMembersOnGenericTypesTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotDeclareStaticMembersOnGenericTypes>();
        }

        [TestMethod]
        public async Task StaticMembersInNonGenericClass_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test
{
    static string field;
    static string Prop => throw null;
    static string Method() => throw null;

    string field2;
    string Prop2 => throw null;
    string Method2() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembersInGenericClass_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test<T>
{
    string field2;
    string Prop2 => throw null;
    string Method2() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldNotReportDiagnostic()
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Field_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test<T>
{
    static string field;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 4, column: 19)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Property_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test<T>
{
    static string Prop => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 4, column: 19)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Method_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
class Test<T>
{
    static string Method() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldReportDiagnostic(line: 4, column: 19)
                  .ValidateAsync();
        }
    }
}
