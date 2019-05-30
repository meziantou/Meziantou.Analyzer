using System.Threading.Tasks;
using Meziantou.Analyzer.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Meziantou.Analyzer.Test.Rules
{
    [TestClass]
    public sealed class MakeClassStaticAnalyzerTests
    {
        private static ProjectBuilder CreateProjectBuilder()
        {
            return new ProjectBuilder()
                .WithAnalyzer<MakeClassStaticAnalyzer>()
                .WithCodeFixProvider<MakeClassStaticFixer>();
        }

        [TestMethod]
        public async Task AbstractClass_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task Inherited_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task InstanceField_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task ImplementInterface_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task StaticMethodAndConstField_DiagnosticAsync()
        {
            const string SourceCode = @"
public class Test
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
                  .ShouldReportDiagnostic(line: 2, column: 14)
                  .ShouldFixCodeWith(CodeFix)
                  .ValidateAsync();
        }

        [TestMethod]
        public async Task ConversionOperator_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task AddOperator_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task ComImport_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task Instantiation_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task MsTestClass_NoDiagnosticAsync()
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

        [TestMethod]
        public async Task SealedClass_NoDiagnostic()
        {
            const string SourceCode = @"
public sealed class [|]Test
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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
    }
}
