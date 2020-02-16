using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Xunit;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    public sealed class MakeClassStaticAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MakeClassStaticAnalyzer>()
                .WithCodeFixProvider<MakeClassStaticFixer>();
        }

        [Fact]
        public async Task AbstractClass_NoDiagnostic()
        {
            const string SourceCode = @"
abstract class AbstractClass
{
    static void A() { }
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Inherited_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    static void A() { }
}

class Test2 : Test { }
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task InstanceField_NoDiagnostic()
        {
            const string SourceCode = @"
class Test4
{
    int _a;
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ImplementInterface_NoDiagnostic()
        {
            const string SourceCode = @"
class Test : ITest
{
}

interface ITest { }
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task StaticMethodAndConstField_Diagnostic()
        {
            const string SourceCode = @"
public class [||]Test
{
    const int a = 10;
    static void A() { }
}";
            const string CodeFix = @"
public static class Test
{
    const int a = 10;
    static void A() { }
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ConversionOperator_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public static implicit operator int(Test _) => 1;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task AddOperator_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public static Test operator +(Test a, Test b) => throw null;
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task ComImport_NoDiagnostic()
        {
            const string SourceCode = @"
[System.Runtime.InteropServices.CoClass(typeof(Test))]
interface ITest
{
}

class Test
{
}";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Instantiation_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    public static void A() => new Test();
}
";
            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task MsTestClass_NoDiagnostic()
        {
            const string SourceCode = @"
[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
class Test
{
}
";
            await CreateProjectBuilder()
                  .AddMSTestApi()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task SealedClass_NoDiagnostic()
        {
            const string SourceCode = @"
public sealed class [||]Test
{
}
";
            const string CodeFix = @"
public static class Test
{
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [Fact]
        public async Task GenericClass_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    static void A<T>() => throw null;
    static void B() => A<Test>();
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task Array_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    static void A() => _ = new Test[0];
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task GenericObjectCreation_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    static void A() => new Test2<Test>();
}

class Test2<T>
{
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task GenericInvocation_NoDiagnostic()
        {
            const string SourceCode = @"
class Test
{
    static void A() => Test2.A<Test>();
}

static class Test2
{
    public static void A<T>() => throw null;
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ValidateAsync();
        }

        [Fact]
        public async Task FixShouldAddStaticBeforePartial()
        {
            const string SourceCode = @"
public partial class [||]Test
{
}
";

            const string CodeFix = @"
public static partial class Test
{
}
";

            await CreateProjectBuilder()
                  .WithSourceCode(SourceCode)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }
    }
}
