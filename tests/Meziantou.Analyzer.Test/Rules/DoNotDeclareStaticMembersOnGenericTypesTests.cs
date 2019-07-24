using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class DoNotDeclareStaticMembersOnGenericTypesTests
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
public class Test
{
    public static string field;
    public static string Prop => throw null;
    public static string Method() => throw null;

    public string field2;
    public string Prop2 => throw null;
    public string Method2() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembersInGenericClass_ShouldNotReportDiagnosticAsync()
        {
            const string SourceCode = @"
public class Test<T>
{
    public string field2;
    public string Prop2 => throw null;
    public string Method2() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Field_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
public class Test<T>
{
    public static string [||]field;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Property_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
public class Test<T>
{
    public static string [||]Prop => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Method_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
public class Test<T>
{
    public static string [||]Method() => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task StaticMembers_Operator_ShouldReportDiagnosticAsync()
        {
            const string SourceCode = @"
public class Test<T>
{
    public static implicit operator Test<T>(int i) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
