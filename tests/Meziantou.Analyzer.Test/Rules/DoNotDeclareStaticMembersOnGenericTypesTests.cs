using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class DoNotDeclareStaticMembersOnGenericTypesTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<DoNotDeclareStaticMembersOnGenericTypes>();
        }

        [Fact]
        public async Task StaticMembersInNonGenericClass_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task StaticMembersInGenericClass_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task StaticMembers_Field_ShouldReportDiagnostic()
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

        [Fact]
        public async Task StaticMembers_Property_ShouldReportDiagnostic()
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

        [Fact]
        public async Task StaticMembers_Method_ShouldNotReportDiagnostic()
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

        [Fact]
        public async Task StaticMembers_Operator_ShouldReportDiagnostic()
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

        [Fact]
        public async Task Const()
        {
            const string SourceCode = @"
public class Test<T>
{    
    public const string PasswordlessSignInPurpose = ""PasswordlessSignIn"";
}";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }
    }
}
